@echo off
:: ============================================
:: Green Hell - Install
:: ============================================
:: Thin wrapper - install body lives in cameraunlock-core/scripts/install-body-melonloader.cmd.

:: --- CONFIG BLOCK ---
set "GAME_ID=green-hell"
set "MOD_DISPLAY_NAME=Green Hell Head Tracking"
set "MOD_DLLS=GreenHellHeadTracking.dll CameraUnlock.Core.dll CameraUnlock.Core.Unity.dll CameraUnlock.Core.Unity.Harmony.dll"
set "MOD_INTERNAL_NAME=GreenHellHeadTracking"
set "MOD_VERSION=1.1.2"
set "STATE_FILE=.headtracking-state.json"
set "FRAMEWORK_TYPE=MelonLoader"
set "MELONLOADER_MARKER=MelonLoader\net35\MelonLoader.dll"
set "MOD_CONTROLS=Controls:&echo   Home/Ctrl+Shift+T - Recenter&echo   End/Ctrl+Shift+Y  - Toggle on/off&echo   PageUp/Ctrl+Shift+G - Toggle 6DOF&echo   PageDown/Ctrl+Shift+H - Toggle yaw mode"
:: MELONLOADER_MARKER tells us whether ML is already installed. Default is
:: net35 (Unity 2017-era games). For IL2CPP / modern Mono games override
:: to MelonLoader\net6\MelonLoader.dll. The bundled MelonLoader version is
:: pinned by vendor/melonloader/MelonLoader.x64.zip; bump via `pixi run update-deps`.
:: --- END CONFIG BLOCK ---

set "WRAPPER_DIR=%~dp0"
set "_BODY=%WRAPPER_DIR%shared\install-body-melonloader.cmd"
if not exist "%_BODY%" set "_BODY=%WRAPPER_DIR%..\cameraunlock-core\scripts\install-body-melonloader.cmd"
if not exist "%_BODY%" (
    echo ERROR: install-body-melonloader.cmd not found in shared\ or ..\cameraunlock-core\scripts\.
    echo If this is a release ZIP, re-download it from GitHub ^(corrupt installer^).
    echo If this is the dev tree, run: git submodule update --init --recursive
    exit /b 1
)
call "%_BODY%" %*
exit /b %errorlevel%