#!/usr/bin/env pwsh
#Requires -Version 5.1
# Refresh vendored mod-loader copies against upstream so every build picks up
# the freshest known-good fallback. Called as a dependency of `pixi run build`.
# Failures are non-fatal (e.g. offline dev, GitHub rate limit) - the existing
# vendored copy is kept.
# See ~/.claude/CLAUDE.md "Vendoring Third-Party Dependencies".

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$ProgressPreference = 'SilentlyContinue'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir

# Locate ModLoaderSetup.psm1: try the submodule first, then fall back to the
# sibling monorepo checkout at ../cameraunlock-core (used during development
# before a submodule bump has shipped new functions).
$moduleCandidates = @(
    (Join-Path $projectDir "cameraunlock-core\powershell\ModLoaderSetup.psm1"),
    (Join-Path $projectDir "..\cameraunlock-core\powershell\ModLoaderSetup.psm1")
)
$modulePath = $null
foreach ($candidate in $moduleCandidates) {
    if (Test-Path $candidate) {
        $content = Get-Content $candidate -Raw
        if ($content -match 'Refresh-VendoredLoader') {
            $modulePath = $candidate
            break
        }
    }
}
if (-not $modulePath) {
    Write-Warning "ModLoaderSetup.psm1 with Refresh-VendoredLoader not found. Run 'pixi run sync' to bump submodule. Keeping existing vendored copy."
    return
}
Import-Module $modulePath -Force

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
    Write-Warning "vendor/melonloader refresh failed ($_). Existing vendored copy will be used."
}

$vendorMlZip = Join-Path $vendorMlDir "MelonLoader.x64.zip"
if (-not (Test-Path $vendorMlZip)) {
    throw "Bundled MelonLoader fallback missing after refresh: $vendorMlZip"
}
