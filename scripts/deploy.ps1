#!/usr/bin/env pwsh
# Deploy built mod to Green Hell MelonLoader Mods folder
# Automatically installs MelonLoader if not present

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = "Stop"
$ProgressPreference = 'SilentlyContinue'

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

$managedPath = Join-Path $gamePath "GH_Data\Managed"

# Ensure libs folder has required DLLs (for building)
$libsPath = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "..\src\GreenHellHeadTracking\libs"
$requiredDlls = @(
    @{ Name = "UnityEngine.dll"; Source = $managedPath },
    @{ Name = "UnityEngine.CoreModule.dll"; Source = $managedPath },
    @{ Name = "UnityEngine.InputLegacyModule.dll"; Source = $managedPath },
    @{ Name = "Assembly-CSharp.dll"; Source = $managedPath }
)

$missingDlls = $requiredDlls | Where-Object { -not (Test-Path (Join-Path $libsPath $_.Name)) }

if ($missingDlls.Count -gt 0) {
    Write-Host "Setting up required DLLs in libs folder..." -ForegroundColor Yellow

    if (-not (Test-Path $libsPath)) {
        New-Item -ItemType Directory -Path $libsPath -Force | Out-Null
    }

    foreach ($dll in $requiredDlls) {
        $srcDll = Join-Path $dll.Source $dll.Name
        $destDll = Join-Path $libsPath $dll.Name

        if (Test-Path $srcDll) {
            Copy-Item $srcDll $destDll -Force
            Write-Host "  Copied: $($dll.Name)" -ForegroundColor Gray
        } else {
            Write-Host "  WARNING: $($dll.Name) not found in game folder" -ForegroundColor Yellow
        }
    }
}

# Install MelonLoader if not present (uses shared module)
$melonResult = Install-MelonLoader -GamePath $gamePath -Architecture x64 -Version '0.6.1'

if (-not $melonResult.AlreadyInstalled -and -not $melonResult.Initialized) {
    Write-Host ""
    Write-Host "After running the game once, run this deploy script again." -ForegroundColor Yellow
    exit 0
}

if (-not (Test-MelonLoaderInitialized -GamePath $gamePath)) {
    Write-Host "MelonLoader installed but not initialized." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Please run the game ONCE to let MelonLoader initialize," -ForegroundColor Yellow
    Write-Host "then run this deploy script again." -ForegroundColor Yellow
    exit 0
}

# Copy MelonLoader DLLs to libs if not present
$melonLibPath = Get-MelonLoaderLibPath -GamePath $gamePath -NetFolder 'net35'
foreach ($dll in (Get-MelonLoaderReferenceDlls)) {
    $destDll = Join-Path $libsPath $dll
    if (-not (Test-Path $destDll)) {
        $srcDll = Join-Path $melonLibPath $dll
        if (Test-Path $srcDll) {
            if (-not (Test-Path $libsPath)) {
                New-Item -ItemType Directory -Path $libsPath -Force | Out-Null
            }
            Copy-Item $srcDll $destDll -Force
            Write-Host "  Copied to libs: $dll" -ForegroundColor Gray
        }
    }
}

$modsPath = Get-MelonLoaderModsPath -GamePath $gamePath
if (-not (Test-Path $modsPath)) {
    New-Item -ItemType Directory -Path $modsPath -Force | Out-Null
    Write-Host "Created Mods folder: $modsPath" -ForegroundColor Gray
}

$buildPath = "src/GreenHellHeadTracking/bin/$Configuration/net472"

# Validate build output exists
if (-not (Test-Path $buildPath)) {
    Write-Host "ERROR: Build output not found at $buildPath" -ForegroundColor Red
    Write-Host "Please run 'pixi run build' first" -ForegroundColor Yellow
    exit 1
}

Write-Host "Deploying GreenHellHeadTracking ($Configuration) to MelonLoader..." -ForegroundColor Green
Write-Host "  Source: $buildPath" -ForegroundColor Gray
Write-Host "  Target: $modsPath" -ForegroundColor Gray

# Copy DLLs
Copy-Item "$buildPath/GreenHellHeadTracking.dll" $modsPath -Force
Copy-Item "$buildPath/CameraUnlock.Core.dll" $modsPath -Force
Copy-Item "$buildPath/CameraUnlock.Core.Unity.dll" $modsPath -Force
Copy-Item "$buildPath/CameraUnlock.Core.Unity.Harmony.dll" $modsPath -Force
Write-Host "  Copied: GreenHellHeadTracking.dll, CameraUnlock.Core.*.dll" -ForegroundColor Gray

# Copy PDB if exists
if (Test-Path "$buildPath/GreenHellHeadTracking.pdb") {
    Copy-Item "$buildPath/GreenHellHeadTracking.pdb" $modsPath -Force
    Write-Host "  Copied: GreenHellHeadTracking.pdb" -ForegroundColor Gray
}

Write-Host '' -ForegroundColor Green
Write-Host "[OK] Deployment complete!" -ForegroundColor Green
Write-Host "DLL location: $modsPath/GreenHellHeadTracking.dll" -ForegroundColor Cyan
Write-Host '' -ForegroundColor Green
Write-Host "Launch Green Hell to test your changes." -ForegroundColor Yellow
