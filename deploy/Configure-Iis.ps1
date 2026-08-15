[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$PhysicalPath,

    [Parameter(Mandatory)]
    [ValidatePattern('^[a-zA-Z0-9.-]+$')]
    [string]$HostName,

    [Parameter(Mandatory)]
    [ValidatePattern('^[A-Fa-f0-9 ]+$')]
    [string]$CertificateThumbprint,

    [string]$SiteName = 'EmpPortal',
    [string]$AppPoolName = 'EmpPortal',
    [ValidateRange(1, 65535)]
    [int]$HttpsPort = 443,
    [string]$GmsaUserName
)

$ErrorActionPreference = 'Stop'
$resolvedPhysicalPath = [System.IO.Path]::GetFullPath($PhysicalPath)
if (-not (Test-Path -LiteralPath $resolvedPhysicalPath -PathType Container)) {
    throw "Physical path does not exist: $resolvedPhysicalPath"
}
if (-not (Test-Path -LiteralPath (Join-Path $resolvedPhysicalPath 'EmpPortal.Web.dll'))) {
    throw 'The target directory does not contain an EmpPortal publish artifact.'
}

$normalizedThumbprint = $CertificateThumbprint.Replace(' ', '').ToUpperInvariant()
$certificate = Get-Item -LiteralPath "Cert:\LocalMachine\My\$normalizedThumbprint" -ErrorAction Stop
if (-not $certificate.HasPrivateKey) { throw 'The HTTPS certificate has no private key.' }
if ($certificate.NotAfter -le (Get-Date)) { throw 'The HTTPS certificate is expired.' }

Import-Module WebAdministration
if (-not $PSCmdlet.ShouldProcess($SiteName, 'Configure IIS site, app pool, authentication, and HTTPS binding')) {
    return
}

if (-not (Test-Path -LiteralPath "IIS:\AppPools\$AppPoolName")) {
    New-WebAppPool -Name $AppPoolName | Out-Null
}
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value ''
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name managedPipelineMode -Value 'Integrated'
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name startMode -Value 'AlwaysRunning'

$aclIdentity = "IIS AppPool\$AppPoolName"
if (-not [string]::IsNullOrWhiteSpace($GmsaUserName)) {
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name processModel.identityType -Value 3
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name processModel.userName -Value $GmsaUserName
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name processModel.password -Value ''
    $aclIdentity = $GmsaUserName
}

if (-not (Test-Path -LiteralPath "IIS:\Sites\$SiteName")) {
    New-Website -Name $SiteName `
        -PhysicalPath $resolvedPhysicalPath `
        -ApplicationPool $AppPoolName `
        -Port 80 `
        -HostHeader $HostName | Out-Null
}
else {
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name physicalPath -Value $resolvedPhysicalPath
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name applicationPool -Value $AppPoolName
}
Set-ItemProperty "IIS:\Sites\$SiteName" -Name applicationDefaults.preloadEnabled -Value $true

$existingHttpsBinding = Get-WebBinding -Name $SiteName -Protocol https |
    Where-Object { $_.bindingInformation -eq "*:$HttpsPort`:$HostName" }
if ($null -eq $existingHttpsBinding) {
    New-WebBinding -Name $SiteName -Protocol https -Port $HttpsPort -HostHeader $HostName -SslFlags 1
}
$sslBindingPath = "IIS:\SslBindings\0.0.0.0!$HttpsPort!$HostName"
if (Test-Path -LiteralPath $sslBindingPath) {
    Remove-Item -LiteralPath $sslBindingPath -Force
}
New-Item -Path $sslBindingPath -Thumbprint $normalizedThumbprint -SSLFlags 1 | Out-Null

Set-WebConfigurationProperty `
    -PSPath 'MACHINE/WEBROOT/APPHOST' `
    -Location $SiteName `
    -Filter 'system.webServer/security/authentication/anonymousAuthentication' `
    -Name enabled `
    -Value $true
Set-WebConfigurationProperty `
    -PSPath 'MACHINE/WEBROOT/APPHOST' `
    -Location $SiteName `
    -Filter 'system.webServer/security/authentication/windowsAuthentication' `
    -Name enabled `
    -Value $true

& icacls.exe $resolvedPhysicalPath /grant "${aclIdentity}:(OI)(CI)(RX)" /T /C | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Failed to grant read/execute permission to the app-pool identity.' }

Start-WebAppPool -Name $AppPoolName
Start-Website -Name $SiteName
Write-Host "IIS site '$SiteName' is configured at https://$HostName`:$HttpsPort/."
