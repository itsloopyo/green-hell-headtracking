#!/usr/bin/env pwsh
# Remove HeadTracking mod from Green Hell (vanilla mode)

$ErrorActionPreference = "Stop"

# Import shared modules
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$sharedModulesPath = Join-Path $projectRoot "cameraunlock-core\powershell"
Import-Module (Join-Path $sharedModulesPath "GamePathDetection.psm1") -Force
Import-Module (Join-Path $sharedModulesPath "ModLoaderSetup.psm1") -Force

$gameId = 'GreenHell'
$config = Get-GameConfig -GameId $gameId

# Find game installation
$gamePath = Find-GamePath -GameId $gameId

if (-not $gamePath) {
    Write-GameNotFoundError -GameName 'Green Hell' -EnvVar $config.EnvVar -SteamFolder $config.SteamFolder
    exit 1
}

Write-Host "Found game at: $gamePath" -ForegroundColor Green

$modsPath = Get-MelonLoaderModsPath -GamePath $gamePath

if (-not (Test-Path $modsPath)) {
    Write-Host "No Mods folder found - already vanilla" -ForegroundColor Gray
    exit 0
}

Write-Host "Removing HeadTracking mod..." -ForegroundColor Yellow

$filesToRemove = @(
    "GreenHellHeadTracking.dll",
    "GreenHellHeadTracking.pdb",
    "CameraUnlock.Core.dll",
    "CameraUnlock.Core.Unity.dll",
    "CameraUnlock.Core.Unity.Harmony.dll"
)

$removed = $false
foreach ($file in $filesToRemove) {
    $filePath = Join-Path $modsPath $file
    if (Test-Path $filePath) {
        Remove-Item $filePath -Force
        Write-Host "  Removed: $file" -ForegroundColor Gray
        $removed = $true
    }
}

if (-not $removed) {
    Write-Host "  No mod files found - already vanilla" -ForegroundColor Gray
} else {
    Write-Host ""
    Write-Host "[OK] Game is now vanilla (mod removed)" -ForegroundColor Green
}
