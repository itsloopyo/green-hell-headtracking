#!/usr/bin/env pwsh
# Populate src/GreenHellHeadTracking/libs with the reference DLLs the build
# needs but that don't ship in the repo:
#   - MelonLoader.dll / 0Harmony.dll: extracted from vendor/melonloader/MelonLoader.x64.zip
# Unity DLLs are resolved by Directory.Build.props directly from the game's
# GH_Data\Managed folder, so we don't copy them here.

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$libsPath = Join-Path $projectRoot 'src/GreenHellHeadTracking/libs'
$vendorZip = Join-Path $projectRoot 'vendor/melonloader/MelonLoader.x64.zip'

if (-not (Test-Path $vendorZip)) {
    Write-Error "Vendored MelonLoader not found at $vendorZip"
    exit 1
}

$needed = @('MelonLoader.dll', '0Harmony.dll')
$missing = $needed | Where-Object { -not (Test-Path (Join-Path $libsPath $_)) }

if ($missing.Count -eq 0) {
    return
}

if (-not (Test-Path $libsPath)) {
    New-Item -ItemType Directory -Path $libsPath -Force | Out-Null
}

$tempDir = Join-Path $env:TEMP ("ghht-melonloader-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
try {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::ExtractToDirectory($vendorZip, $tempDir)

    foreach ($dll in $missing) {
        $src = Join-Path $tempDir "MelonLoader/net35/$dll"
        if (-not (Test-Path $src)) {
            Write-Error "$dll not found in vendored zip at MelonLoader/net35/"
            exit 1
        }
        Copy-Item $src (Join-Path $libsPath $dll) -Force
        Write-Host "  Bootstrapped: $dll" -ForegroundColor Gray
    }
} finally {
    Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}
