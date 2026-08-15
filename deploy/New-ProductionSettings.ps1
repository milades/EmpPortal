[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$DestinationPath,

    [string]$PortalDatabaseConnectionString = 'Server=SQL01;Database=EmpPortal;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True',
    [string]$HostName = 'portal.corp.example',
    [string]$BootstrapAdministratorUpn = 'portal.admin@corp.example',
    [string]$DomainFqdn = 'corp.example',
    [string]$BaseDn = 'DC=corp,DC=example',
    [string[]]$DomainControllers = @('dc01.corp.example', 'dc02.corp.example'),
    [string]$DataProtectionCertificateThumbprint = '92A1D39B6C0E4F4B9A83D7E2C5F1680A4B7D9E31',
    [string]$JwtSigningCertificateThumbprint = '4C8E7A10B2D569F3A61C90E4D73B8F25A0C6E912'
)

$ErrorActionPreference = 'Stop'
$templatePath = Join-Path $PSScriptRoot 'appsettings.Production.example.json'
$resolvedDestination = [System.IO.Path]::GetFullPath($DestinationPath)
$destinationDirectory = Split-Path -Parent $resolvedDestination
if (-not (Test-Path -LiteralPath $destinationDirectory -PathType Container)) {
    throw "Destination directory does not exist: $destinationDirectory"
}

$settings = Get-Content -LiteralPath $templatePath -Raw | ConvertFrom-Json
$settings.ConnectionStrings.PortalDatabase = $PortalDatabaseConnectionString
$settings.AllowedHosts = $HostName
$settings.BootstrapAdministrator.Upn = $BootstrapAdministratorUpn
$settings.ActiveDirectory.DomainFqdn = $DomainFqdn
$settings.ActiveDirectory.BaseDn = $BaseDn
$settings.ActiveDirectory.DomainControllers = @($DomainControllers)
$settings.DataProtection.CertificateThumbprint = $DataProtectionCertificateThumbprint.Replace(' ', '').ToUpperInvariant()
$settings.Jwt.Issuer = "https://$HostName"
$settings.Jwt.SigningCertificateThumbprint = $JwtSigningCertificateThumbprint.Replace(' ', '').ToUpperInvariant()

$settings | ConvertTo-Json -Depth 10 |
    Set-Content -LiteralPath $resolvedDestination -Encoding utf8

Write-Host "Production settings created at: $resolvedDestination"
Write-Warning 'The sample certificate thumbprints must match installed LocalMachine/My certificates before the first Production start.'
Write-Host 'The existing Stimulsoft license setting was not copied or changed by this script.'
