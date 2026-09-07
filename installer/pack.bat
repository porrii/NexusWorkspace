@echo off
setlocal EnableExtensions
title NexusWorkspace - empaquetado del instalador

rem ============================================================================
rem  Genera el instalable de Windows para NexusWorkspace.
rem
rem    pack.bat            -> instalador Velopack (Setup.exe + full/delta)
rem    pack.bat nozip      -> zip portable self-contained (sin auto-update)
rem    pack.bat [nozip] 1.2.3   -> version explicita (por defecto: Directory.Build.props)
rem
rem  Todo el toolchain va a D:\Programs, nada en C:  (igual que build.bat).
rem  La primera ejecucion descarga el runtime win-x64 y, en modo Velopack, la
rem  herramienta 'vpk' -> puede tardar varios minutos.
rem ============================================================================

set "TOOLS_BASE=D:\Programs"
set "DOTNET_ROOT=%TOOLS_BASE%\dotnet"
set "DOTNET_TOOLS=%TOOLS_BASE%\dotnet-tools"
set "NUGET_PACKAGES=%TOOLS_BASE%\nuget-packages"
set "DOTNET_CLI_HOME=%TOOLS_BASE%\dotnet-home"
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "DOTNET_NOLOGO=1"
set "PATH=%DOTNET_ROOT%;%DOTNET_TOOLS%;%PATH%"

rem --- rutas (repo root = carpeta padre de este .bat) ------------------------
pushd "%~dp0.."
set "REPO=%CD%"
popd
set "PROJECT=%REPO%\src\NexusWorkspace.Desktop\NexusWorkspace.Desktop.csproj"
set "PROPS=%REPO%\Directory.Build.props"
set "RID=win-x64"
set "PUBDIR=%REPO%\installer\publish"
set "OUTDIR=%REPO%\installer\releases"
set "LOGDIR=%REPO%\installer\logs"
if not exist "%LOGDIR%" mkdir "%LOGDIR%"

rem --- argumentos ----------------------------------------------------------------
set "MODE=vpk"
if /i "%~1"=="nozip" set "MODE=nozip"& shift
set "VERSION=%~1"
if "%VERSION%"=="" for /f "tokens=3 delims=<>" %%v in ('findstr /i "<Version>" "%PROPS%"') do set "VERSION=%%v"
if "%VERSION%"=="" set "VERSION=1.0.0"

echo ==== NexusWorkspace installer ==========================================
echo   Repo:     %REPO%
echo   Modo:     %MODE%
echo   Version:  %VERSION%   RID: %RID%
echo   Salida:   %OUTDIR%
echo =======================================================================

where dotnet >nul 2>nul
if errorlevel 1 echo [X] No se encuentra dotnet en %DOTNET_ROOT%. Ejecuta build.bat primero.& exit /b 1

rem --- 1) publish self-contained ---------------------------------------------
echo [1/3] dotnet publish self-contained (%RID%) ...
if exist "%PUBDIR%" rmdir /s /q "%PUBDIR%"
dotnet publish "%PROJECT%" -c Release -r %RID% --self-contained true -p:Version=%VERSION% -o "%PUBDIR%" > "%LOGDIR%\publish.log" 2>&1
if errorlevel 1 echo [X] Fallo en publish. Revisa %LOGDIR%\publish.log& exit /b 1
if not exist "%PUBDIR%\NexusWorkspace.Desktop.exe" echo [X] publish no genero NexusWorkspace.Desktop.exe. Revisa %LOGDIR%\publish.log& exit /b 1

if not exist "%OUTDIR%" mkdir "%OUTDIR%"

rem --- 2) empaquetar --------------------------------------------------------------
if /i "%MODE%"=="nozip" goto :nozip

echo [2/3] Comprobando la herramienta Velopack (vpk) ...
if not exist "%DOTNET_TOOLS%\vpk.exe" (
  echo        Instalando vpk ... (la primera vez tarda)
  dotnet tool install --tool-path "%DOTNET_TOOLS%" vpk > "%LOGDIR%\vpk-install.log" 2>&1
)
if not exist "%DOTNET_TOOLS%\vpk.exe" (
  echo [X] No se pudo instalar vpk. Revisa %LOGDIR%\vpk-install.log
  echo     Alternativa sin auto-update:  pack.bat nozip %VERSION%
  exit /b 1
)

echo [3/3] vpk pack ...
"%DOTNET_TOOLS%\vpk.exe" pack --packId NexusWorkspace --packTitle "NexusWorkspace" --packAuthors "Ivan" --packVersion %VERSION% --packDir "%PUBDIR%" --mainExe NexusWorkspace.Desktop.exe --outputDir "%OUTDIR%" > "%LOGDIR%\vpk-pack.log" 2>&1
if errorlevel 1 echo [X] Fallo en vpk pack. Revisa %LOGDIR%\vpk-pack.log& exit /b 1
echo.
dir /b "%OUTDIR%"
echo.
echo Instalador:  %OUTDIR%\NexusWorkspace-win-Setup.exe
echo Los datos del usuario (%%APPDATA%%\NexusWorkspace) se conservan al actualizar.
exit /b 0

:nozip
echo [2/3] Comprimiendo la carpeta publicada ...
set "ZIP=%OUTDIR%\NexusWorkspace-%VERSION%-win-x64.zip"
if exist "%ZIP%" del /q "%ZIP%"
powershell -NoProfile -ExecutionPolicy Bypass -Command "Compress-Archive -Path '%PUBDIR%\*' -DestinationPath '%ZIP%' -Force" > "%LOGDIR%\zip.log" 2>&1
if errorlevel 1 echo [X] Fallo al comprimir. Revisa %LOGDIR%\zip.log& exit /b 1
echo [3/3] Listo.
echo.
dir /b "%OUTDIR%"
echo.
echo Portable: descomprimir y ejecutar NexusWorkspace.Desktop.exe. No requiere nada mas.
exit /b 0
