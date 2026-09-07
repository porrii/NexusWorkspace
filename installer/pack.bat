@echo off
setlocal EnableExtensions EnableDelayedExpansion
title NexusWorkspace - empaquetado del instalador

rem ============================================================================
rem  Genera el instalador de Windows (Velopack): Setup.exe + paquete completo
rem  + delta, en installer\releases\. Reutilizable para cada version.
rem
rem  Todo el toolchain va a D:\Programs (nada en C:), igual que build.bat.
rem  Uso:   pack.bat  [version]      (por defecto: la <Version> de Directory.Build.props)
rem ============================================================================

set "TOOLS_BASE=D:\Programs"
set "DOTNET_ROOT=%TOOLS_BASE%\dotnet"
set "DOTNET_TOOLS=%TOOLS_BASE%\dotnet-tools"
set "NUGET_PACKAGES=%TOOLS_BASE%\nuget-packages"
set "DOTNET_CLI_HOME=%TOOLS_BASE%\dotnet-home"
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "DOTNET_NOLOGO=1"
set "PATH=%DOTNET_ROOT%;%DOTNET_TOOLS%;%PATH%"

rem  %~dp0 already ends with "installer\", so the repo root is its parent.
for %%i in ("%~dp0..") do set "REPO=%%~fi"
set "PROJECT=%REPO%\src\NexusWorkspace.Desktop\NexusWorkspace.Desktop.csproj"
set "RID=win-x64"
set "PUBDIR=%REPO%\installer\publish"
set "OUTDIR=%REPO%\installer\releases"
for /f "tokens=1-4 delims=/:., " %%a in ("%DATE% %TIME%") do set "STAMP=%%a%%b%%c-%%d"
set "LOGDIR=%REPO%\installer\logs\%STAMP%"
mkdir "%LOGDIR%" 2>nul

rem  Optional: "pack.bat nozip [version]" skips Velopack and just publishes + zips
rem  (framework-dependent-free, no auto-update, always verifiable).
set "MODE=full"
if /i "%~1"=="nozip" ( set "MODE=nozip" & shift )

set "VERSION=%~1"
if "%VERSION%"=="" (
  for /f "tokens=3 delims=<>" %%v in ('findstr /i "<Version>" "%REPO%\Directory.Build.props"') do set "VERSION=%%v"
)
if "%VERSION%"=="" set "VERSION=1.0.0"

echo ==== NexusWorkspace installer =======================================
echo   Repo:     %REPO%
echo   Version:  %VERSION%
echo   RID:      %RID%
echo   Salida:   %OUTDIR%
echo   Logs:     %LOGDIR%
echo ====================================================================

where dotnet >nul 2>nul || (echo [X] No se encuentra dotnet en %DOTNET_ROOT%. Ejecuta build.bat primero. & exit /b 1)

echo [1/3] dotnet publish (self-contained, %RID%) ...
if exist "%PUBDIR%" rmdir /s /q "%PUBDIR%"
dotnet publish "%PROJECT%" -c Release -r %RID% --self-contained true ^
  -p:PublishSingleFile=false -p:Version=%VERSION% -o "%PUBDIR%" > "%LOGDIR%\publish.log" 2>&1
if errorlevel 1 (echo [X] Fallo en publish. Revisa %LOGDIR%\publish.log & exit /b 1)
mkdir "%OUTDIR%" 2>nul

if /i "%MODE%"=="nozip" (
  echo [2/3] Comprimiendo la carpeta publicada ^(sin auto-update^) ...
  set "ZIP=%OUTDIR%\NexusWorkspace-%VERSION%-win-x64.zip"
  if exist "!ZIP!" del /q "!ZIP!"
  powershell -NoProfile -Command "Compress-Archive -Path '%PUBDIR%\*' -DestinationPath '!ZIP!' -Force" > "%LOGDIR%\zip.log" 2>&1
  if errorlevel 1 (echo [X] Fallo al comprimir. Revisa %LOGDIR%\zip.log & exit /b 1)
  echo [3/3] Listo.  &  dir /b "%OUTDIR%"
  echo Portable ^(descomprimir y ejecutar NexusWorkspace.Desktop.exe^). Requiere nada mas.
  endlocal & exit /b 0
)

echo [2/3] Comprobando la herramienta Velopack (vpk)...
"%DOTNET_TOOLS%\vpk.exe" --help >nul 2>nul
if errorlevel 1 (
  echo        Instalando vpk en "%DOTNET_TOOLS%" ^(la primera vez puede tardar varios minutos^) ...
  dotnet tool install --tool-path "%DOTNET_TOOLS%" vpk > "%LOGDIR%\vpk-install.log" 2>&1
  if errorlevel 1 (
    echo [X] No se pudo instalar vpk. Revisa %LOGDIR%\vpk-install.log
    echo     Alternativa sin auto-update:  pack.bat nozip %VERSION%
    exit /b 1
  )
)

echo [3/3] vpk pack ...
"%DOTNET_TOOLS%\vpk.exe" pack ^
  --packId NexusWorkspace ^
  --packTitle "NexusWorkspace" ^
  --packAuthors "Ivan" ^
  --packVersion %VERSION% ^
  --packDir "%PUBDIR%" ^
  --mainExe NexusWorkspace.Desktop.exe ^
  --outputDir "%OUTDIR%" > "%LOGDIR%\vpk-pack.log" 2>&1
if errorlevel 1 (echo [X] Fallo en vpk pack. Revisa %LOGDIR%\vpk-pack.log & exit /b 1)

echo Listo.
echo.
dir /b "%OUTDIR%"
echo.
echo Instalador:  %OUTDIR%\NexusWorkspace-win-Setup.exe
echo Los datos del usuario (%%APPDATA%%\NexusWorkspace) se conservan entre versiones.
endlocal
