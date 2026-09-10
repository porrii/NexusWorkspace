@echo off
setlocal EnableExtensions
title NexusWorkspace - empaquetado del instalador

rem ============================================================================
rem  Genera el instalable de Windows para NexusWorkspace.
rem
rem    pack.bat                  -> instalador Velopack (Setup.exe + full/delta)
rem    pack.bat nozip            -> zip portable self-contained (sin auto-update)
rem    pack.bat [nozip] 1.2.3    -> version explicita (por defecto: Directory.Build.props)
rem
rem  El SDK de .NET lo resuelve  scripts\dotnet-bootstrap.bat : usa el que ya
rem  tengas instalado (PATH, Archivos de programa, perfil de usuario, D:\Programs)
rem  y, si no hay ninguno compatible, instala una copia privada en <repo>\.dotnet
rem  sin permisos de administrador. La primera vez tambien descarga el runtime
rem  win-x64 y, en modo Velopack, la herramienta 'vpk'.
rem
rem  NOTA: las variables internas llevan prefijo NX_ para no chocar con
rem  propiedades de MSBuild que se leen del entorno (OutDir, Version, ...).
rem  Antes 'set OUTDIR=...' se convertia en la propiedad MSBuild OutDir y
rem  desviaba TODA la compilacion a installer\releases.
rem ============================================================================

rem --- rutas (repo root = carpeta padre de este .bat) ------------------------
pushd "%~dp0.."
set "NX_REPO=%CD%"
popd
set "NX_PROJECT=%NX_REPO%\src\NexusWorkspace.Desktop\NexusWorkspace.Desktop.csproj"
set "NX_PROPS=%NX_REPO%\Directory.Build.props"
set "NX_RID=win-x64"
set "NX_PUBDIR=%NX_REPO%\installer\publish"
set "NX_RELDIR=%NX_REPO%\installer\releases"
set "NX_LOGDIR=%NX_REPO%\installer\logs"
if not exist "%NX_LOGDIR%" mkdir "%NX_LOGDIR%"

rem --- .NET SDK (lo localiza o instala una copia privada) --------------------
if not exist "%NX_REPO%\scripts\dotnet-bootstrap.bat" (
  echo [X] Falta scripts\dotnet-bootstrap.bat. Haz 'git pull' y reintenta.& exit /b 1
)
call "%NX_REPO%\scripts\dotnet-bootstrap.bat"
if errorlevel 1 exit /b 1

rem --- argumentos ----------------------------------------------------------------
set "NX_MODE=vpk"
if /i "%~1"=="nozip" set "NX_MODE=nozip"& shift
set "NX_VERSION=%~1"
if "%NX_VERSION%"=="" for /f "tokens=3 delims=<>" %%v in ('findstr /i "<Version>" "%NX_PROPS%"') do set "NX_VERSION=%%v"
if "%NX_VERSION%"=="" set "NX_VERSION=1.0.0"

echo ==== NexusWorkspace installer ==========================================
echo   Repo:     %NX_REPO%
echo   Modo:     %NX_MODE%
echo   Version:  %NX_VERSION%   RID: %NX_RID%
echo   Salida:   %NX_RELDIR%
echo =======================================================================

rem --- 1) publish self-contained ---------------------------------------------
echo [1/3] dotnet publish self-contained (%NX_RID%) ...
if exist "%NX_PUBDIR%" rmdir /s /q "%NX_PUBDIR%"
"%DOTNET%" publish "%NX_PROJECT%" -c Release -r %NX_RID% --self-contained true -p:Version=%NX_VERSION% -o "%NX_PUBDIR%" > "%NX_LOGDIR%\publish.log" 2>&1
if errorlevel 1 echo [X] Fallo en publish. Revisa %NX_LOGDIR%\publish.log& exit /b 1
if not exist "%NX_PUBDIR%\NexusWorkspace.Desktop.exe" echo [X] publish no genero NexusWorkspace.Desktop.exe. Revisa %NX_LOGDIR%\publish.log& exit /b 1

rem  releases siempre limpio (para deltas se conservaria el .nupkg previo;
rem  de momento cada release es completo).
if exist "%NX_RELDIR%" rmdir /s /q "%NX_RELDIR%"
mkdir "%NX_RELDIR%"

rem --- 2) empaquetar --------------------------------------------------------------
if /i "%NX_MODE%"=="nozip" goto :nozip

echo [2/3] Comprobando la herramienta Velopack (vpk) ...
if not exist "%DOTNET_TOOLS%\vpk.exe" (
  echo        Instalando vpk ... (la primera vez tarda)
  "%DOTNET%" tool install --tool-path "%DOTNET_TOOLS%" vpk > "%NX_LOGDIR%\vpk-install.log" 2>&1
)
if not exist "%DOTNET_TOOLS%\vpk.exe" (
  echo [X] No se pudo instalar vpk. Revisa %NX_LOGDIR%\vpk-install.log
  echo     Alternativa sin auto-update:  pack.bat nozip %NX_VERSION%
  exit /b 1
)

echo [3/3] vpk pack ...
"%DOTNET_TOOLS%\vpk.exe" pack --packId NexusWorkspace --packTitle "NexusWorkspace" --packAuthors "Ivan" --packVersion %NX_VERSION% --packDir "%NX_PUBDIR%" --mainExe NexusWorkspace.Desktop.exe --icon "%NX_REPO%\src\NexusWorkspace.Desktop\app.ico" --outputDir "%NX_RELDIR%" > "%NX_LOGDIR%\vpk-pack.log" 2>&1
if errorlevel 1 (
  echo [X] Fallo en vpk pack. Ultimas lineas de %NX_LOGDIR%\vpk-pack.log :
  powershell -NoProfile -Command "Get-Content -LiteralPath '%NX_LOGDIR%\vpk-pack.log' -Tail 20"
  exit /b 1
)
echo.
dir /b "%NX_RELDIR%"
echo.
echo Instalador:  %NX_RELDIR%\NexusWorkspace-win-Setup.exe
echo Los datos del usuario (%%APPDATA%%\NexusWorkspace) se conservan al actualizar.
exit /b 0

:nozip
echo [2/3] Comprimiendo la carpeta publicada ...
set "NX_ZIP=%NX_RELDIR%\NexusWorkspace-%NX_VERSION%-win-x64.zip"
if exist "%NX_ZIP%" del /q "%NX_ZIP%"
powershell -NoProfile -ExecutionPolicy Bypass -Command "Compress-Archive -Path '%NX_PUBDIR%\*' -DestinationPath '%NX_ZIP%' -Force" > "%NX_LOGDIR%\zip.log" 2>&1
if errorlevel 1 echo [X] Fallo al comprimir. Revisa %NX_LOGDIR%\zip.log& exit /b 1
echo [3/3] Listo.
echo.
dir /b "%NX_RELDIR%"
echo.
echo Portable: descomprimir y ejecutar NexusWorkspace.Desktop.exe. No requiere nada mas.
exit /b 0
