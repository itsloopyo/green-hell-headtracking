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

# Vendor refresh runs as a dependency of `pixi run build`, so by the time we
# package we already have an up-to-date vendor/melonloader/. We only validate
# the expected artifacts exist here.
$vendorMlDir = Join-Path $projectDir "vendor\melonloader"
$vendorMlZip = Join-Path $vendorMlDir "MelonLoader.x64.zip"
if (-not (Test-Path $vendorMlZip)) {
    throw "Bundled MelonLoader missing: $vendorMlZip. Run 'pixi run update-deps' first."
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

# Stamp launcher-manifest.json with the real release version and copy it
# into the installer ZIP root. The launcher reads this file to decide how
# to stage the mod.
$manifestSource = Join-Path $projectDir "launcher-manifest.json"
if (-not (Test-Path $manifestSource)) {
    Write-Host "ERROR: launcher-manifest.json not found at repo root ($manifestSource)" -ForegroundColor Red
    exit 1
}
$manifestJson = Get-Content $manifestSource -Raw | ConvertFrom-Json
$manifestJson.mod_info.version = $version
$manifestDest = Join-Path $ghStagingDir "launcher-manifest.json"
# `Set-Content -Encoding UTF8` on Windows PowerShell 5.1 writes a BOM
# (EF BB BF) which serde_json rejects with "expected value at line 1
# column 1". Write through the .NET API with an explicit no-BOM encoder.
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText(
    $manifestDest,
    ($manifestJson | ConvertTo-Json -Depth 10),
    $utf8NoBom
)
Write-Host "  launcher-manifest.json (v$version)" -ForegroundColor Green

# Copy mod DLLs to plugins subfolder
$pluginsDir = Join-Path $ghStagingDir "plugins"
New-Item -ItemType Directory -Path $pluginsDir -Force | Out-Null

foreach ($dll in $modDlls) {
    Copy-Item (Join-Path $buildOutputDir $dll) -Destination $pluginsDir -Force
    Write-Host "  plugins/$dll" -ForegroundColor Green
}

# Bundle vendored MelonLoader (Apache-2.0, see THIRD-PARTY-NOTICES.md) as the
# install-time source of truth. install.cmd extracts this zip directly.
$ghVendorDir = Join-Path $ghStagingDir "vendor\melonloader"
New-Item -ItemType Directory -Path $ghVendorDir -Force | Out-Null
# Apache-2.0 section 4(a) requires the licence to travel with the redistributed
# work, so a missing LICENSE fails the build rather than shipping the loader bare.
foreach ($vendorFile in @("MelonLoader.x64.zip", "LICENSE", "README.md")) {
    $src = Join-Path $vendorMlDir $vendorFile
    if (-not (Test-Path $src)) {
        throw "Required vendor file missing: $src. The vendored loader must ship with its licence and provenance note. Run 'pixi run update-deps'."
    }
    Copy-Item $src -Destination $ghVendorDir -Force
    Write-Host "  vendor/melonloader/$vendorFile" -ForegroundColor Green
}

# Copy documentation. LICENSE and THIRD-PARTY-NOTICES.md are licence obligations
# on a binary distribution, not optional docs: a silent skip would turn a
# compliance failure into a green build.
$docFiles = @("README.md", "LICENSE", "CHANGELOG.md", "THIRD-PARTY-NOTICES.md")
foreach ($doc in $docFiles) {
    $docPath = Join-Path $projectDir $doc
    if (-not (Test-Path $docPath)) {
        throw "Required document not found: $doc. Every published ZIP is a binary distribution and must carry it."
    }
    Copy-Item $docPath -Destination $ghStagingDir -Force
    Write-Host "  $doc" -ForegroundColor Green
}

# install.cmd / uninstall.cmd resolve the game via shared/find-game.ps1.
# Bundle that shim alongside them so the release ZIP is self-contained.
Copy-SharedBundle -StagingDir $ghStagingDir

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

# The Nexus ZIP is a binary distribution too: the licences of everything
# compiled into or bundled with the payload require their notices to travel
# with it, so LICENSE and THIRD-PARTY-NOTICES.md ship at its root.
foreach ($noticeDoc in @('LICENSE', 'THIRD-PARTY-NOTICES.md', 'README.md')) {
    $noticeSrc = Join-Path $projectDir $noticeDoc
    if (-not (Test-Path $noticeSrc)) {
        throw "Required notice file not found: $noticeDoc. Every published ZIP is a binary distribution and must carry it."
    }
    Copy-Item $noticeSrc -Destination $nexusStagingDir -Force
    Write-Host "  $noticeDoc" -ForegroundColor Green
}
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
