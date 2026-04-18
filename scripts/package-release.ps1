#!/usr/bin/env pwsh
#Requires -Version 5.1
# Custom packaging for Green Hell Head Tracking (MelonLoader mod)
# Produces two ZIPs:
#   - GreenHellHeadTracking-v{version}-installer.zip (GitHub Release: install.cmd + plugins/ + docs)
#   - GreenHellHeadTracking-v{version}-nexus.zip     (Nexus Mods: extract-to-game-folder layout)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$ProgressPreference = 'SilentlyContinue'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir

Import-Module (Join-Path $projectDir "cameraunlock-core\powershell\ReleaseWorkflow.psm1") -Force
Import-Module (Join-Path $projectDir "cameraunlock-core\powershell\ModLoaderSetup.psm1") -Force

$csprojPath = Join-Path $projectDir "src\GreenHellHeadTracking\GreenHellHeadTracking.csproj"
$buildOutputDir = Join-Path $projectDir "src\GreenHellHeadTracking\bin\Release\net472"
$modDlls = @("GreenHellHeadTracking.dll", "CameraUnlock.Core.dll", "CameraUnlock.Core.Unity.dll", "CameraUnlock.Core.Unity.Harmony.dll")

$version = Get-CsprojVersion $csprojPath

Write-Host "=== Green Hell Head Tracking - Package Release ===" -ForegroundColor Magenta
Write-Host ""
Write-Host "Version: $version" -ForegroundColor Cyan
Write-Host ""

$releaseDir = Join-Path $projectDir "release"
$scriptsDir = Join-Path $projectDir "scripts"

# Create release directory
if (-not (Test-Path $releaseDir)) {
    New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
}

# Validate all DLLs exist upfront
foreach ($dll in $modDlls) {
    $dllPath = Join-Path $buildOutputDir $dll
    if (-not (Test-Path $dllPath)) {
        throw "Required DLL not found: $dllPath"
    }
}

# Refresh vendored MelonLoader from upstream so every release ZIP ships with
# the freshest known-good fallback. install.cmd still tries upstream first at
# user install time. See ~/.claude/CLAUDE.md "Vendoring Third-Party Dependencies".
$vendorMlDir = Join-Path $projectDir "vendor\melonloader"
Write-Host "Refreshing vendor/melonloader from upstream..." -ForegroundColor Cyan
try {
    Refresh-VendoredLoader `
        -Name 'melonloader' `
        -OutputDir $vendorMlDir `
        -OutputFileName 'MelonLoader.x64.zip' `
        -Owner 'LavaGang' -Repo 'MelonLoader' `
        -VersionPrefix 'v0.6.' `
        -AssetPattern '^MelonLoader\.x64\.zip$' | Out-Null
} catch {
    Write-Warning "Could not refresh vendor/melonloader from upstream ($_). Existing vendored copy will be used."
}
$vendorMlZip = Join-Path $vendorMlDir "MelonLoader.x64.zip"
if (-not (Test-Path $vendorMlZip)) {
    throw "Bundled MelonLoader fallback missing after refresh: $vendorMlZip"
}

# Validate required scripts
foreach ($script in @("install.cmd", "uninstall.cmd")) {
    $scriptPath = Join-Path $scriptsDir $script
    if (-not (Test-Path $scriptPath)) {
        throw "Required script not found: $scriptPath"
    }
}

# --- GitHub Release ZIP (with installer) ---

Write-Host "--- GitHub Release ZIP ---" -ForegroundColor Yellow
Write-Host ""

$ghStagingDir = Join-Path $releaseDir "staging-github"
if (Test-Path $ghStagingDir) { Remove-Item -Recurse -Force $ghStagingDir }
New-Item -ItemType Directory -Path $ghStagingDir -Force | Out-Null

# Copy install/uninstall scripts
foreach ($script in @("install.cmd", "uninstall.cmd")) {
    Copy-Item (Join-Path $scriptsDir $script) -Destination $ghStagingDir -Force
    Write-Host "  $script" -ForegroundColor Green
}

# Copy mod DLLs to plugins subfolder
$pluginsDir = Join-Path $ghStagingDir "plugins"
New-Item -ItemType Directory -Path $pluginsDir -Force | Out-Null

foreach ($dll in $modDlls) {
    Copy-Item (Join-Path $buildOutputDir $dll) -Destination $pluginsDir -Force
    Write-Host "  plugins/$dll" -ForegroundColor Green
}

# Bundle vendored MelonLoader (Apache-2.0, see THIRD-PARTY-NOTICES.md) as a
# fallback when install.cmd cannot reach upstream.
$ghVendorDir = Join-Path $ghStagingDir "vendor\melonloader"
New-Item -ItemType Directory -Path $ghVendorDir -Force | Out-Null
foreach ($vendorFile in @("MelonLoader.x64.zip", "LICENSE", "README.md", "fetch-latest.ps1")) {
    $src = Join-Path $vendorMlDir $vendorFile
    if (Test-Path $src) {
        Copy-Item $src -Destination $ghVendorDir -Force
        Write-Host "  vendor/melonloader/$vendorFile" -ForegroundColor Green
    } elseif ($vendorFile -in @("MelonLoader.x64.zip", "fetch-latest.ps1")) {
        throw "Required vendor file missing: $src"
    }
}

# Copy documentation
$docFiles = @("README.md", "LICENSE", "CHANGELOG.md", "THIRD-PARTY-NOTICES.md")
foreach ($doc in $docFiles) {
    $docPath = Join-Path $projectDir $doc
    if (Test-Path $docPath) {
        Copy-Item $docPath -Destination $ghStagingDir -Force
        Write-Host "  $doc" -ForegroundColor Green
    } elseif ($doc -eq "LICENSE") {
        Write-Host "  WARNING: $doc not found" -ForegroundColor Yellow
    }
}

$ghZipName = "GreenHellHeadTracking-v$version-installer.zip"
$ghZipPath = Join-Path $releaseDir $ghZipName
if (Test-Path $ghZipPath) { Remove-Item $ghZipPath -Force }

Write-Host ""
Write-Host "Creating GitHub ZIP..." -ForegroundColor Cyan

Push-Location $ghStagingDir
try {
    Compress-Archive -Path ".\*" -DestinationPath $ghZipPath -Force
} finally {
    Pop-Location
}
Remove-Item -Recurse -Force $ghStagingDir

$ghZipSize = (Get-Item $ghZipPath).Length / 1KB
Write-Host ("  $ghZipPath ({0:N1} KB)" -f $ghZipSize) -ForegroundColor Green

# --- Nexus Mods ZIP (extract-to-game-folder) ---

Write-Host ""
Write-Host "--- Nexus Mods ZIP ---" -ForegroundColor Yellow
Write-Host ""

$nexusStagingDir = Join-Path $releaseDir "staging-nexus"
if (Test-Path $nexusStagingDir) { Remove-Item -Recurse -Force $nexusStagingDir }

# Mirror game directory structure: Mods/
$nexusModsDir = Join-Path $nexusStagingDir "Mods"
New-Item -ItemType Directory -Path $nexusModsDir -Force | Out-Null

foreach ($dll in $modDlls) {
    Copy-Item (Join-Path $buildOutputDir $dll) -Destination $nexusModsDir -Force
    Write-Host "  Mods/$dll" -ForegroundColor Green
}

$nexusZipName = "GreenHellHeadTracking-v$version-nexus.zip"
$nexusZipPath = Join-Path $releaseDir $nexusZipName
if (Test-Path $nexusZipPath) { Remove-Item $nexusZipPath -Force }

Write-Host ""
Write-Host "Creating Nexus ZIP..." -ForegroundColor Cyan

Push-Location $nexusStagingDir
try {
    Compress-Archive -Path ".\*" -DestinationPath $nexusZipPath -Force
} finally {
    Pop-Location
}
Remove-Item -Recurse -Force $nexusStagingDir

$nexusZipSize = (Get-Item $nexusZipPath).Length / 1KB
Write-Host ("  $nexusZipPath ({0:N1} KB)" -f $nexusZipSize) -ForegroundColor Green

# --- Summary ---

Write-Host ""
Write-Host "=== Package Complete ===" -ForegroundColor Magenta
Write-Host ""
Write-Host ("GitHub Release: $ghZipPath ({0:N1} KB)" -f $ghZipSize) -ForegroundColor Green
Write-Host ("Nexus Mods:     $nexusZipPath ({0:N1} KB)" -f $nexusZipSize) -ForegroundColor Green

# Output both zip paths for CI capture (one per line)
Write-Output $ghZipPath
Write-Output $nexusZipPath
