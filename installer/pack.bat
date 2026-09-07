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

set "REPO=%~dp0.."
set "PROJECT=%REPO%\src\NexusWorkspace.Desktop\NexusWorkspace.Desktop.csproj"
set "RID=win-x64"
set "PUBDIR=%REPO%\installer\publish"
set "OUTDIR=%REPO%\installer\releases"
set "LOGDIR=%REPO%\installer\logs\%DATE:/=-%_%TIME::=-%"
set "LOGDIR=%LOGDIR: =0%"
mkdir "%LOGDIR%" 2>nul

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

echo [1/4] Comprobando la herramienta Velopack (vpk)...
vpk --help >nul 2>nul
if errorlevel 1 (
  echo        Instalando vpk en "%DOTNET_TOOLS%" ...
  dotnet tool install --tool-path "%DOTNET_TOOLS%" vpk > "%LOGDIR%\vpk-install.log" 2>&1
  if errorlevel 1 (echo [X] No se pudo instalar vpk. Revisa %LOGDIR%\vpk-install.log & exit /b 1)
) else (
  echo        vpk ya disponible.
)

echo [2/4] dotnet publish (self-contained, %RID%) ...
if exist "%PUBDIR%" rmdir /s /q "%PUBDIR%"
dotnet publish "%PROJECT%" -c Release -r %RID% --self-contained true ^
  -p:PublishSingleFile=false -p:Version=%VERSION% -o "%PUBDIR%" > "%LOGDIR%\publish.log" 2>&1
if errorlevel 1 (echo [X] Fallo en publish. Revisa %LOGDIR%\publish.log & exit /b 1)

echo [3/4] vpk pack ...
mkdir "%OUTDIR%" 2>nul
vpk pack ^
  --packId NexusWorkspace ^
  --packTitle "NexusWorkspace" ^
  --packAuthors "Ivan" ^
  --packVersion %VERSION% ^
  --packDir "%PUBDIR%" ^
  --mainExe NexusWorkspace.Desktop.exe ^
  --outputDir "%OUTDIR%" > "%LOGDIR%\vpk-pack.log" 2>&1
if errorlevel 1 (echo [X] Fallo en vpk pack. Revisa %LOGDIR%\vpk-pack.log & exit /b 1)

echo [4/4] Listo.
echo.
dir /b "%OUTDIR%"
echo.
echo Instalador:  %OUTDIR%\NexusWorkspace-win-Setup.exe
echo Los datos del usuario (%%APPDATA%%\NexusWorkspace) se conservan entre versiones.
endlocal
