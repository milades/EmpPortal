[CmdletBinding()]
param(
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$solutionPath = Join-Path $repositoryRoot 'EmpPortal.sln'
$webProjectPath = Join-Path $repositoryRoot 'src\EmpPortal.Web\EmpPortal.Web.csproj'
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $artifactName = 'win-x64-' + (Get-Date).ToUniversalTime().ToString('yyyyMMdd-HHmmss')
    $OutputPath = Join-Path $repositoryRoot "artifacts\publish\$artifactName"
}
$resolvedOutputPath = [System.IO.Path]::GetFullPath($OutputPath)
if ((Test-Path -LiteralPath $resolvedOutputPath) -and
    (Get-ChildItem -LiteralPath $resolvedOutputPath -Force | Select-Object -First 1)) {
    throw "Output directory must be empty or new: $resolvedOutputPath"
}

Push-Location $repositoryRoot
try {
    dotnet restore $solutionPath --locked-mode --ignore-failed-sources -p:NuGetAudit=false
    if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }

    dotnet build $solutionPath -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }

    dotnet test $solutionPath -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'dotnet test failed.' }

    dotnet publish $webProjectPath `
        -c Release `
        -r win-x64 `
        --self-contained false `
        --no-restore `
        -o $resolvedOutputPath
    if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

    $hashes = Get-ChildItem -LiteralPath $resolvedOutputPath -File -Recurse |
        Sort-Object FullName |
        ForEach-Object {
            $relativePath = $_.FullName.Substring(
                $resolvedOutputPath.TrimEnd('\').Length).TrimStart('\')
            [pscustomobject]@{
                Path = $relativePath
                Sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
            }
        }
    $hashes | ConvertTo-Json -Depth 3 |
        Set-Content -LiteralPath (Join-Path $resolvedOutputPath 'sha256-manifest.json') -Encoding utf8

    Write-Host "Publish artifact created at: $resolvedOutputPath"
}
finally {
    Pop-Location
}
