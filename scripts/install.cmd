@echo off
:: ============================================
:: Green Hell Head Tracking - Install
:: ============================================
:: Based on cameraunlock-core/scripts/templates/install.cmd
:: NOTE: Uses MelonLoader instead of BepInEx
:: ============================================

:: --- CONFIG BLOCK ---
set "MOD_DISPLAY_NAME=Green Hell Head Tracking"
set "GAME_EXE=GH.exe"
set "GAME_DISPLAY_NAME=Green Hell"
set "STEAM_FOLDER_NAME=Green Hell"
set "ENV_VAR_NAME=GREEN_HELL_PATH"
set "MOD_DLLS=GreenHellHeadTracking.dll CameraUnlock.Core.dll CameraUnlock.Core.Unity.dll CameraUnlock.Core.Unity.Harmony.dll"
set "MOD_INTERNAL_NAME=GreenHellHeadTracking"
set "MOD_VERSION=1.0.0"
set "STATE_FILE=.headtracking-state.json"
:: MelonLoader version is resolved dynamically by vendor/melonloader/fetch-latest.ps1
:: (pinned to v0.6.x range). The vendored fallback zip at vendor/melonloader/MelonLoader.x64.zip
:: is refreshed to latest-within-range at release build time.
set "MOD_CONTROLS=Controls (nav cluster / chord):&echo   Home     / Ctrl+Shift+T  Recenter&echo   End      / Ctrl+Shift+Y  Toggle tracking&echo   PageUp   / Ctrl+Shift+G  Toggle 6DOF position&echo   PageDown / Ctrl+Shift+H  Toggle yaw mode (world/local)"
set "GOG_IDS="
set "SEARCH_DIRS="
:: --- END CONFIG BLOCK ---

call :main %*
set "_EC=%errorlevel%"
echo.
pause
exit /b %_EC%

:main
setlocal enabledelayedexpansion

echo.
echo === %MOD_DISPLAY_NAME% - Install ===
echo.

set "SCRIPT_DIR=%~dp0"
set "GAME_PATH="

:: --- Find game path ---

:: Check command line argument
if not "%~1"=="" (
    if exist "%~1\%GAME_EXE%" (
        set "GAME_PATH=%~1"
        goto :found_game
    )
    echo ERROR: %GAME_EXE% not found at: "%~1"
    echo.
    exit /b 1
)

:: Check environment variable
if defined %ENV_VAR_NAME% (
    call set "_ENV_PATH=%%%ENV_VAR_NAME%%%"
    if exist "!_ENV_PATH!\%GAME_EXE%" (
        set "GAME_PATH=!_ENV_PATH!"
        goto :found_game
    )
)

:: Check Steam
call :find_steam_game
if defined GAME_PATH goto :found_game

:: Check GOG
call :find_gog_game
if defined GAME_PATH goto :found_game

:: Check Epic
call :find_epic_game
if defined GAME_PATH goto :found_game

:: Check common directories
call :find_game_in_dirs
if defined GAME_PATH goto :found_game

echo ERROR: Could not find %GAME_DISPLAY_NAME% installation.
echo.
echo Please either:
echo   1. Set %ENV_VAR_NAME% environment variable to your game folder
echo   2. Run: install.cmd "C:\path\to\game"
echo.
exit /b 1

:found_game
echo Game found: %GAME_PATH%
echo.

:: --- Check if game is running ---
tasklist /fi "imagename eq %GAME_EXE%" 2>nul | findstr /i "%GAME_EXE%" >nul 2>&1
if not errorlevel 1 (
    echo ERROR: %GAME_DISPLAY_NAME% is currently running.
    echo Please close the game before installing.
    echo.
    exit /b 1
)

:: --- Check MelonLoader ---
:: Second positional arg `UNATTENDED` means the launcher is running us -
:: skip the interactive "type install to continue" gate, which would
:: loop forever against a null stdin. MelonLoader initializes on first
:: game launch whether or not plugins are already sitting in Mods\, so
:: there's no ordering hazard from deploying straight away.
set "UNATTENDED="
if /i "%~2"=="UNATTENDED" set "UNATTENDED=1"

if not exist "%GAME_PATH%\MelonLoader\net35\MelonLoader.dll" (
    echo MelonLoader not found. Installing...
    echo.
    call :install_melonloader
    if errorlevel 1 exit /b 1
    echo.
    if defined UNATTENDED (
        echo MelonLoader installed. It will initialize on first game launch.
    ) else (
        call :prompt_melonloader_init
    )
) else (
    echo Existing MelonLoader detected, skipping loader install, deploying plugin only.
)
echo.

:: --- Deploy mod files ---
echo Deploying mod files...

set "MODS_PATH=%GAME_PATH%\Mods"
set "DLL_DIR=%SCRIPT_DIR%plugins"

if not exist "%MODS_PATH%" mkdir "%MODS_PATH%"

set "DEPLOY_FAILED=0"
for %%f in (%MOD_DLLS%) do (
    if exist "%DLL_DIR%\%%f" (
        copy /y "%DLL_DIR%\%%f" "%MODS_PATH%\" >nul
        echo   Deployed %%f
    ) else (
        echo   ERROR: %%f not found in plugins folder
        set "DEPLOY_FAILED=1"
    )
)

if "!DEPLOY_FAILED!"=="1" (
    echo.
    echo ========================================
    echo   Deployment Failed!
    echo ========================================
    echo.
    exit /b 1
)

:: --- Update state file ---
:: Preserve installed_by_us flag from previous state
set "WE_INSTALLED=false"
if exist "%GAME_PATH%\%STATE_FILE%" (
    findstr /c:"installed_by_us" "%GAME_PATH%\%STATE_FILE%" 2>nul | findstr /c:"true" >nul 2>&1
    if not errorlevel 1 set "WE_INSTALLED=true"
)

> "%GAME_PATH%\%STATE_FILE%" (
    echo {
    echo   "framework": {
    echo     "type": "MelonLoader",
    echo     "installed_by_us": !WE_INSTALLED!
    echo   },
    echo   "mod": {
    echo     "name": "%MOD_INTERNAL_NAME%",
    echo     "version": "%MOD_VERSION%"
    echo   }
    echo }
)

echo.
echo ========================================
echo   Deployment Complete!
echo ========================================
echo.
echo %MOD_DISPLAY_NAME% has been deployed to:
echo   %MODS_PATH%
echo.
echo Start the game to use the mod!
if defined MOD_CONTROLS (
    echo.
    echo !MOD_CONTROLS!
)
echo.
exit /b 0

:: ============================================
:: Find game in Steam libraries
:: ============================================
:find_steam_game
set "STEAM_PATH="

:: Get Steam install path from registry (64-bit)
for /f "tokens=2*" %%a in ('reg query "HKLM\SOFTWARE\WOW6432Node\Valve\Steam" /v InstallPath 2^>nul') do set "STEAM_PATH=%%b"

:: Try 32-bit registry
if not defined STEAM_PATH (
    for /f "tokens=2*" %%a in ('reg query "HKLM\SOFTWARE\Valve\Steam" /v InstallPath 2^>nul') do set "STEAM_PATH=%%b"
)

:: Check default Steam library
if defined STEAM_PATH (
    if exist "%STEAM_PATH%\steamapps\common\%STEAM_FOLDER_NAME%\%GAME_EXE%" (
        set "GAME_PATH=%STEAM_PATH%\steamapps\common\%STEAM_FOLDER_NAME%"
        exit /b 0
    )
)

:: Parse libraryfolders.vdf for additional Steam library paths
if defined STEAM_PATH (
    set "VDF_FILE=%STEAM_PATH%\steamapps\libraryfolders.vdf"
    if exist "!VDF_FILE!" (
        for /f "tokens=1,2 delims=	 " %%a in ('findstr /c:"\"path\"" "!VDF_FILE!" 2^>nul') do (
            set "_LIB_PATH=%%~b"
            set "_LIB_PATH=!_LIB_PATH:\\=\!"
            if exist "!_LIB_PATH!\steamapps\common\%STEAM_FOLDER_NAME%\%GAME_EXE%" (
                set "GAME_PATH=!_LIB_PATH!\steamapps\common\%STEAM_FOLDER_NAME%"
                exit /b 0
            )
        )
    )
)

exit /b 1

:: ============================================
:: Find game in GOG registry
:: ============================================
:find_gog_game
if not defined GOG_IDS exit /b 1
for %%g in (%GOG_IDS%) do (
    for /f "tokens=2*" %%a in ('reg query "HKLM\SOFTWARE\WOW6432Node\GOG.com\Games\%%g" /v path 2^>nul') do (
        if exist "%%b\%GAME_EXE%" ( set "GAME_PATH=%%b" & exit /b 0 )
    )
    for /f "tokens=2*" %%a in ('reg query "HKLM\SOFTWARE\GOG.com\Games\%%g" /v path 2^>nul') do (
        if exist "%%b\%GAME_EXE%" ( set "GAME_PATH=%%b" & exit /b 0 )
    )
)
exit /b 1

:: ============================================
:: Find game in Epic Games manifests
:: ============================================
:find_epic_game
set "_EPIC_MANIFESTS=%ProgramData%\Epic\EpicGamesLauncher\Data\Manifests"
if not exist "%_EPIC_MANIFESTS%" exit /b 1
for %%m in ("%_EPIC_MANIFESTS%\*.item") do (
    for /f "usebackq delims=" %%l in ("%%m") do (
        set "_EL=%%l"
        if not "!_EL:InstallLocation=!"=="!_EL!" (
            set "_EL=!_EL:*InstallLocation=!"
            set "_EL=!_EL:~4!"
            set "_EL=!_EL:~0,-2!"
            set "_EL=!_EL:\\=\!"
            if exist "!_EL!\%GAME_EXE%" ( set "GAME_PATH=!_EL!" & exit /b 0 )
        )
    )
)
exit /b 1

:: ============================================
:: Find game by scanning common directories
:: ============================================
:find_game_in_dirs
if not defined SEARCH_DIRS exit /b 1
for %%d in (%SEARCH_DIRS%) do (
    if exist "%%~d\%GAME_EXE%" ( set "GAME_PATH=%%~d" & exit /b 0 )
    for /f "delims=" %%p in ('dir /b /ad "%%~d" 2^>nul') do (
        if exist "%%~d\%%p\%GAME_EXE%" ( set "GAME_PATH=%%~d\%%p" & exit /b 0 )
        for /f "delims=" %%s in ('dir /b /ad "%%~d\%%p" 2^>nul') do (
            if exist "%%~d\%%p\%%s\%GAME_EXE%" ( set "GAME_PATH=%%~d\%%p\%%s" & exit /b 0 )
        )
    )
)
exit /b 1

:: ============================================
:: Interactive MelonLoader init gate (manual-install flow only).
:: Extracted from the main install block so the label can live at the
:: top level - cmd labels inside parenthesized blocks interact badly
:: with `goto`.
:: ============================================
:prompt_melonloader_init
color 0E
echo ========================================
echo   MelonLoader installed - action required
echo ========================================
echo.
echo MelonLoader was just installed but needs to initialize first.
echo.
echo   1. Start %GAME_DISPLAY_NAME%
echo   2. Wait until you reach the main menu
echo   3. Close the game
echo   4. Come back here and type "install" to continue
echo.
:melonloader_gate
set "_CONFIRM="
set /p "_CONFIRM=Type install to continue: "
if /i not "!_CONFIRM!"=="install" goto :melonloader_gate
echo.
color
exit /b 0

:: ============================================
:: Install MelonLoader (upstream-first, fall back to vendored copy)
:: See ~/.claude/CLAUDE.md "Vendoring Third-Party Dependencies".
:: ============================================
:install_melonloader
set "VENDOR_DIR=%SCRIPT_DIR%vendor\melonloader"
set "VENDOR_ZIP=%VENDOR_DIR%\MelonLoader.x64.zip"
set "FETCH_SCRIPT=%VENDOR_DIR%\fetch-latest.ps1"
set "ML_ZIP=%TEMP%\MelonLoader_install.zip"
set "LOADER_SOURCE="

if exist "%FETCH_SCRIPT%" (
    echo   Trying upstream MelonLoader, latest within range...
    powershell -NoProfile -ExecutionPolicy Bypass -File "%FETCH_SCRIPT%" -OutputPath "%ML_ZIP%" >nul 2>&1
    if not errorlevel 1 (
        set "LOADER_SOURCE=%ML_ZIP%"
        set "USED_UPSTREAM=1"
        echo   Using upstream MelonLoader.
    )
)

if not defined LOADER_SOURCE (
    if not exist "%VENDOR_ZIP%" (
        echo   ERROR: Upstream unreachable AND bundled fallback missing at:
        echo     %VENDOR_ZIP%
        echo   The installer ZIP is corrupt. Re-download the release.
        exit /b 1
    )
    set "LOADER_SOURCE=%VENDOR_ZIP%"
    echo   Upstream unreachable, using bundled fallback copy.
)

echo   Extracting MelonLoader to game directory...
:: Use the full path to Windows' built-in bsdtar. If the user launched us
:: from a shell whose PATH contains git-bash / MSYS2 first (common on dev
:: machines), a bare `tar` resolves to MSYS tar, which treats `C:` in
:: `-C "C:\..."` as an SSH host ("tar: Cannot connect to C: resolve failed").
"%SystemRoot%\System32\tar.exe" -xf "%LOADER_SOURCE%" -C "%GAME_PATH%"
if errorlevel 1 (
    echo   ERROR: Extraction failed.
    if defined USED_UPSTREAM del "%ML_ZIP%" 2>nul
    exit /b 1
)
if defined USED_UPSTREAM del "%ML_ZIP%" 2>nul

:: Create Mods directory
if not exist "%GAME_PATH%\Mods" mkdir "%GAME_PATH%\Mods"

:: Write state file marking that we installed MelonLoader
> "%GAME_PATH%\%STATE_FILE%" (
    echo {
    echo   "framework": {
    echo     "type": "MelonLoader",
    echo     "installed_by_us": true
    echo   }
    echo }
)

echo   MelonLoader installed successfully!
exit /b 0
