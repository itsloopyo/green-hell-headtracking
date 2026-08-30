#!/usr/bin/env pwsh
# Single source of truth for green-hell's build dependencies.
#
# Both the local dev loop (`pixi run package` -> restore -> bootstrap) and CI
# (`pixi run package`) call THIS script, so a build that passes locally builds
# identically on a runner - there is no second, drifting copy of this logic in
# the workflow YAML. It populates src/GreenHellHeadTracking/libs entirely from
# files in the repo, with NO Green Hell install required:
#
#   - MelonLoader.dll / 0Harmony.dll : extracted from the vendored MelonLoader zip
#   - UnityEngine.dll, UnityEngine.UI.dll and the empty module / Assembly-CSharp
#     reference shells : compiled by cameraunlock-core/csharp/stubs/build-unity-stubs.ps1
#     from the shared stub sources there (our own API-only declarations - zero
#     Unity binaries enter the repo or the build)
#
# The build references these stubs via Directory.Build.props (UnityEnginePath ->
# libs), never the game, so local and CI compile against byte-identical assemblies.

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$libsPath = Join-Path $projectRoot 'src/GreenHellHeadTracking/libs'
$vendorZip = Join-Path $projectRoot 'vendor/melonloader/MelonLoader.x64.zip'
$stubBuilder = Join-Path $projectRoot 'cameraunlock-core/csharp/stubs/build-unity-stubs.ps1'

if (-not (Test-Path $vendorZip)) { throw "Vendored MelonLoader not found at $vendorZip" }
if (-not (Test-Path $stubBuilder)) {
    throw "Shared stub builder not found at $stubBuilder. Run 'git submodule update --init'."
}
New-Item -ItemType Directory -Path $libsPath -Force | Out-Null

Write-Host "Bootstrapping build dependencies (no game install required)..." -ForegroundColor Cyan

# Start from a clean libs/. On a CI runner libs/ is empty (gitignored); locally
# it may hold stale DLLs from a past deploy.ps1 against a real install. Wiping
# them is what makes a local build reproduce the runner instead of silently
# picking up game DLLs. Nothing in libs/ is tracked, so the wipe strands nothing.
Get-ChildItem -Path $libsPath -Force | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

# --- MelonLoader (net35) from the vendored zip ---
Add-Type -AssemblyName System.IO.Compression.FileSystem
$tempDir = Join-Path $env:TEMP ("ghht-ml-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
try {
    [System.IO.Compression.ZipFile]::ExtractToDirectory($vendorZip, $tempDir)
    foreach ($dll in @('MelonLoader.dll', '0Harmony.dll')) {
        $src = Join-Path $tempDir "MelonLoader/net35/$dll"
        if (-not (Test-Path $src)) { throw "$dll not found in vendored zip at MelonLoader/net35/" }
        Copy-Item $src (Join-Path $libsPath $dll) -Force
        Write-Host "  MelonLoader: $dll" -ForegroundColor Gray
    }
} finally {
    Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}

# --- Unity reference stubs, from the shared sources in the core submodule ---
# -EmptyModule is spelled out rather than defaulted because this mod needs one
# entry the fleet default does not carry: Assembly-CSharp, the game's own script
# assembly. Nothing here binds a type out of it - the csproj references it only
# so Harmony can name the methods it patches - so an empty shell is the whole of
# it. UnityEngine.AnimationModule is dropped for the mirror reason: neither this
# csproj nor CameraUnlock.Core.Unity references it.
& $stubBuilder -OutputPath $libsPath -TargetFramework net472 -EmptyModule `
    'UnityEngine.CoreModule', 'UnityEngine.InputLegacyModule', 'UnityEngine.IMGUIModule', `
    'UnityEngine.PhysicsModule', 'UnityEngine.UIModule', 'UnityEngine.TextRenderingModule', `
    'Assembly-CSharp'
if ($LASTEXITCODE -ne 0) { throw "Stub build failed" }

Write-Host "Build dependencies ready." -ForegroundColor Green
