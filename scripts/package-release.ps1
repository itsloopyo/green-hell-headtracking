#!/usr/bin/env pwsh
#Requires -Version 5.1
# Thin wrapper: calls shared packaging script with Green Hell values.

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir

& "$projectDir/cameraunlock-core/scripts/package-bepinex-mod.ps1" `
    -ModName "GreenHellHeadTracking" `
    -CsprojPath "src/GreenHellHeadTracking/GreenHellHeadTracking.csproj" `
    -BuildOutputDir "src/GreenHellHeadTracking/bin/Release/net472" `
    -ModDlls @("GreenHellHeadTracking.dll","CameraUnlock.Core.dll","CameraUnlock.Core.Unity.dll","CameraUnlock.Core.Unity.Harmony.dll") `
    -ProjectRoot $projectDir
