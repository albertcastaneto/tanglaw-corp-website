# Creates the TanglawSite Umbraco 17 project and copies the sample files into it.
# Run from this folder in PowerShell:   .\setup.ps1
# Requires the .NET 10 SDK:             dotnet --list-sdks   (should show 10.x)

$ErrorActionPreference = "Stop"
$projectName = "TanglawSite"
$umbracoVersion = "17.0.0"   # Umbraco 17 LTS; update packages to the latest 17.x patch afterwards if you like

if (-not (dotnet --list-sdks | Select-String "^10\.")) {
    Write-Error ".NET 10 SDK not found. Install it from https://dotnet.microsoft.com/download/dotnet/10.0"
}

if (Test-Path $projectName) {
    Write-Error "Folder '$projectName' already exists. Delete it first if you want a fresh install."
}

Write-Host "Installing Umbraco templates $umbracoVersion..." -ForegroundColor Cyan
dotnet new install "Umbraco.Templates::$umbracoVersion" --force

Write-Host "Creating Umbraco project (SQLite, unattended install)..." -ForegroundColor Cyan
dotnet new umbraco -n $projectName `
    --friendly-name "Administrator" `
    --email "admin@example.com" `
    --password "Tanglaw@2026!" `
    --development-database-type SQLite

Write-Host "Copying sample site files..." -ForegroundColor Cyan
Copy-Item -Path ".\overlay\*" -Destination ".\$projectName" -Recurse -Force

Write-Host ""
Write-Host "Done. Next steps:" -ForegroundColor Green
Write-Host "  cd $projectName"
Write-Host "  dotnet run"
Write-Host "Then open the URL shown in the console. Backoffice: /umbraco"
Write-Host "Login: admin@example.com / Tanglaw@2026!  (change this password after first login)"
