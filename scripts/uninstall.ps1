#!/usr/bin/env pwsh
# Uninstall HeadTracking mod from Green Hell

$ErrorActionPreference = "Stop"

# Detect Green Hell installation path
$gamePath = if ($env:GREEN_HELL_PATH -and (Test-Path $env:GREEN_HELL_PATH)) {
    $env:GREEN_HELL_PATH
} elseif (Test-Path 'C:/Program Files (x86)/Steam/steamapps/common/Green Hell') {
    'C:/Program Files (x86)/Steam/steamapps/common/Green Hell'
} elseif (Test-Path 'C:/Program Files/Steam/steamapps/common/Green Hell') {
    'C:/Program Files/Steam/steamapps/common/Green Hell'
} else {
    Write-Host 'ERROR: Could not find Green Hell installation.' -ForegroundColor Red
    exit 1
}

$modsPath = Join-Path $gamePath "Mods"

Write-Host "Uninstalling HeadTracking mod..." -ForegroundColor Yellow
Write-Host "  Game path: $gamePath" -ForegroundColor Gray

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
    Write-Host "  No mod files found - already uninstalled" -ForegroundColor Gray
}

Write-Host '' -ForegroundColor Green
Write-Host "HeadTracking mod uninstalled" -ForegroundColor Green
Write-Host "MelonLoader remains intact for other mods" -ForegroundColor Gray
Write-Host "Run 'pixi run deploy' to reinstall" -ForegroundColor Yellow
