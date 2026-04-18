#!/usr/bin/env pwsh
#Requires -Version 5.1
# ============================================================================
# vendor/melonloader/fetch-latest.ps1 (Green Hell)
# ============================================================================
# Fetches the latest MelonLoader 0.6.x x64 release from upstream and writes
# the zip to $OutputPath. Exits non-zero on any failure so install.cmd can
# fall back to the bundled vendor/melonloader/MelonLoader.x64.zip.
#
# Self-contained: the user's extracted installer ZIP does not contain
# cameraunlock-core/. Equivalent logic used at package time lives in
# Invoke-FetchLatestLoader (cameraunlock-core/powershell/ModLoaderSetup.psm1).
# ============================================================================

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

# --- CONFIG BLOCK ---------------------------------------------------------
$Owner           = 'LavaGang'
$Repo            = 'MelonLoader'
$VersionPrefix   = 'v0.6.'                           # pin major.minor range
$AssetPattern    = '^MelonLoader\.x64\.zip$'
$AllowPrerelease = $false
$TimeoutSec      = 30
# --- END CONFIG BLOCK -----------------------------------------------------

$headers = @{ "User-Agent" = "CameraUnlock-HeadTracking" }
$outputDir = Split-Path -Parent $OutputPath
if ($outputDir -and -not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

try {
    $apiUrl = "https://api.github.com/repos/$Owner/$Repo/releases?per_page=50"
    $releases = Invoke-RestMethod -Uri $apiUrl -Headers $headers -TimeoutSec $TimeoutSec

    $release = $releases | Where-Object {
        $_.tag_name.StartsWith($VersionPrefix) -and
        ($AllowPrerelease -or -not $_.prerelease)
    } | Select-Object -First 1

    if (-not $release) {
        throw "No upstream release matches $VersionPrefix for $Owner/$Repo."
    }

    $asset = $release.assets | Where-Object { $_.name -match $AssetPattern } | Select-Object -First 1
    if (-not $asset) {
        throw "Release $($release.tag_name) has no asset matching '$AssetPattern'."
    }

    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $OutputPath -UseBasicParsing -TimeoutSec $TimeoutSec -Headers $headers

    $sha = (Get-FileHash -Path $OutputPath -Algorithm SHA256).Hash.ToLower()
    Write-Host "fetch-latest: tag=$($release.tag_name) asset=$($asset.name) sha256=$($sha.Substring(0,12))..."
    exit 0
} catch {
    Write-Error "fetch-latest: $_"
    exit 1
}
