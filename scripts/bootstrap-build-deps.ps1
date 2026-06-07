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
#   - UnityEngine.dll                : compiled from the checked-in UnityStubs.cs
#                                      (our own API-only stub source - zero Unity
#                                      binaries enter the repo or the build)
#   - empty per-module reference assemblies (UnityEngine.*Module, UnityEngine.UI,
#     Assembly-CSharp): satisfy the csproj `<Reference HintPath>` entries; every
#     actual type lives in the UnityEngine.dll stub above.
#
# The build references these stubs via Directory.Build.props (UnityEnginePath ->
# libs), never the game, so local and CI compile against byte-identical assemblies.

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$libsPath = Join-Path $projectRoot 'src/GreenHellHeadTracking/libs'
$vendorZip = Join-Path $projectRoot 'vendor/melonloader/MelonLoader.x64.zip'
$stubSource = Join-Path $libsPath 'UnityStubs.cs'

if (-not (Test-Path $vendorZip)) { throw "Vendored MelonLoader not found at $vendorZip" }
if (-not (Test-Path $stubSource)) { throw "UnityStubs.cs not found at $stubSource" }
New-Item -ItemType Directory -Path $libsPath -Force | Out-Null

Write-Host "Bootstrapping build dependencies (no game install required)..." -ForegroundColor Cyan

# Start from a clean libs/ - keep only the tracked stub source. On a CI runner
# libs/ is empty (gitignored); locally it may hold stale DLLs from a past
# deploy.ps1 against a real install. Wiping them is what makes a local build
# byte-for-byte reproduce the runner instead of silently picking up game DLLs.
Get-ChildItem -Path $libsPath -Force |
    Where-Object { $_.Name -ne 'UnityStubs.cs' } |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

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

# --- Unity reference stubs, compiled from our own UnityStubs.cs ---
# EnableDefaultCompileItems=false so each stub assembly compiles ONLY its
# declared source: UnityEngine.dll gets every type from UnityStubs.cs, and the
# module shells stay genuinely empty (the SDK would otherwise glob every *.cs in
# libs/ into all of them, redefining the Unity types in 8 assemblies).
function Build-Stub([string]$assemblyName, [string]$compileItem) {
    $proj = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>11</LangVersion>
    <AssemblyName>$assemblyName</AssemblyName>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <NoWarn>CS0169;CS0649;CS0067;CS0660;CS0661</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$compileItem" />
  </ItemGroup>
</Project>
"@
    $projPath = Join-Path $libsPath "Stub_$assemblyName.csproj"
    $proj | Out-File -FilePath $projPath -Encoding utf8
    dotnet build $projPath -c Release -o $libsPath --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "Failed to build stub assembly $assemblyName" }
    Remove-Item $projPath -ErrorAction SilentlyContinue
    Write-Host "  Stub: $assemblyName.dll" -ForegroundColor Gray
}

Build-Stub 'UnityEngine' 'UnityStubs.cs'

$emptySourcePath = Join-Path $libsPath 'EmptyStub.cs'
'// Empty stub assembly' | Out-File -FilePath $emptySourcePath -Encoding utf8
$modules = @(
    'UnityEngine.CoreModule', 'UnityEngine.InputLegacyModule', 'UnityEngine.IMGUIModule',
    'UnityEngine.PhysicsModule', 'UnityEngine.UIModule', 'UnityEngine.TextRenderingModule',
    'UnityEngine.UI', 'Assembly-CSharp'
)
foreach ($m in $modules) { Build-Stub $m 'EmptyStub.cs' }

# Cleanup compiler droppings so only the reference DLLs remain in libs/.
Remove-Item $emptySourcePath -ErrorAction SilentlyContinue
Remove-Item (Join-Path $libsPath '*.deps.json') -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $libsPath '*.pdb') -Force -ErrorAction SilentlyContinue

Write-Host "Build dependencies ready." -ForegroundColor Green
