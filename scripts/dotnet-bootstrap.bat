@echo off
rem ===========================================================================
rem  dotnet-bootstrap.bat
rem
rem  Deja preparado un .NET SDK compatible con global.json para el resto de
rem  scripts .bat del repositorio, funcione en el equipo que funcione y lo
rem  tenga el usuario donde lo tenga (o no lo tenga).
rem
rem  Uso, desde otro .bat:
rem       call "RUTA_DEL_REPO\scripts\dotnet-bootstrap.bat"
rem       if errorlevel 1 exit /b 1
rem
rem  Al volver quedan definidas en el entorno del script que llamo:
rem       DOTNET        ruta completa al dotnet.exe que se debe usar
rem       DOTNET_ROOT   carpeta de ese dotnet.exe
rem       DOTNET_TOOLS  carpeta para  "dotnet tool install --tool-path"
rem       PATH          con DOTNET_ROOT y DOTNET_TOOLS al principio
rem       DOTNET_NOLOGO / DOTNET_CLI_TELEMETRY_OPTOUT /
rem       DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 1
rem
rem  Como decide:
rem    1. Un DOTNET_ROOT heredado que apunte a algo inexistente se descarta:
rem       esa es la causa tipica del error enganoso  "Requested SDK 9.0.100 /
rem       No .NET SDKs were found".
rem    2. Se prueban, por orden, las ubicaciones habituales de .NET: la copia
rem       privada del repo, lo que haya en PATH, D:\Programs (PC de desarrollo),
rem       Archivos de programa y el perfil del usuario. Un candidato vale si
rem       "dotnet --version" ejecutado dentro del repo termina sin error, es
rem       decir, cumple global.json + rollForward.
rem    3. Si ninguno sirve, se instala una copia privada del canal indicado en
rem       la carpeta  .dotnet  del repo con el instalador oficial (sin admin,
rem       sin tocar el PATH global ni el resto del equipo). La siguiente
rem       ejecucion ya la reutiliza.
rem ===========================================================================
setlocal EnableExtensions EnableDelayedExpansion

pushd "%~dp0.."
set "_DN_REPO=%CD%"
popd

set "_DN_CHANNEL=9.0"
set "_DN_LOCAL=%_DN_REPO%\.dotnet"
set "_DN_DEVROOT=D:\Programs\dotnet"
set "_DN_NUGET_IN=%NUGET_PACKAGES%"
set "_DN_CLIHOME_IN=%DOTNET_CLI_HOME%"
set "_DN_FOUND="

rem --- 1) un DOTNET_ROOT heredado y roto se descarta -----------------------
if defined DOTNET_ROOT if not exist "%DOTNET_ROOT%\dotnet.exe" (
  echo   [dotnet] Aviso: la variable DOTNET_ROOT apunta a una ruta sin dotnet.exe; se ignora.
  set "DOTNET_ROOT="
)

rem --- 2) lista de candidatos, por orden de preferencia --------------------
set "_DN_LIST="%_DN_LOCAL%\dotnet.exe""
if defined DOTNET_ROOT set "_DN_LIST=!_DN_LIST! "%DOTNET_ROOT%\dotnet.exe""
for /f "delims=" %%p in ('where dotnet 2^>nul') do set "_DN_LIST=!_DN_LIST! "%%~p""
set "_DN_LIST=!_DN_LIST! "%_DN_DEVROOT%\dotnet.exe""
if defined ProgramW6432 set "_DN_LIST=!_DN_LIST! "%ProgramW6432%\dotnet\dotnet.exe""
if defined ProgramFiles set "_DN_LIST=!_DN_LIST! "%ProgramFiles%\dotnet\dotnet.exe""
if defined LocalAppData  set "_DN_LIST=!_DN_LIST! "%LocalAppData%\Microsoft\dotnet\dotnet.exe""
set "_DN_LIST=!_DN_LIST! "%USERPROFILE%\.dotnet\dotnet.exe""

for %%c in (!_DN_LIST!) do (
  if not defined _DN_FOUND if exist "%%~c" (
    pushd "%_DN_REPO%"
    "%%~c" --version >nul 2>nul && set "_DN_FOUND=%%~c"
    popd
  )
)

if defined _DN_FOUND goto :resolved

rem --- 3) instalar una copia privada en la carpeta .dotnet del repo --------
echo.
echo   [dotnet] No hay ningun .NET SDK que cumpla global.json canal %_DN_CHANNEL%.
echo   [dotnet] Se instala una copia privada, sin admin, en:
echo            %_DN_LOCAL%
echo   [dotnet] Si prefieres usar el tuyo, cancela e instala:  winget install Microsoft.DotNet.SDK.9
echo.
set "_DN_PS1=%TEMP%\dotnet-install-%RANDOM%.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; [Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1' -OutFile '%_DN_PS1%'; & '%_DN_PS1%' -Channel '%_DN_CHANNEL%' -InstallDir '%_DN_LOCAL%' -NoPath"
set "_DN_RC=!ERRORLEVEL!"
del "%_DN_PS1%" >nul 2>nul
if not exist "%_DN_LOCAL%\dotnet.exe" (
  echo.
  echo   [dotnet] ERROR: no se pudo instalar .NET automaticamente. Codigo !_DN_RC!.
  echo   [dotnet] Instala el SDK de .NET %_DN_CHANNEL% a mano y reintenta:
  echo   [dotnet]     winget install Microsoft.DotNet.SDK.9
  echo   [dotnet]     o descargalo de  https://aka.ms/dotnet/download
  exit /b 1
)
pushd "%_DN_REPO%"
"%_DN_LOCAL%\dotnet.exe" --version >nul 2>nul
set "_DN_RC=!ERRORLEVEL!"
popd
if not "!_DN_RC!"=="0" (
  echo   [dotnet] ERROR: la copia recien instalada no cumple global.json. Revisa el archivo.
  exit /b 1
)
set "_DN_FOUND=%_DN_LOCAL%\dotnet.exe"

:resolved
for %%d in ("%_DN_FOUND%") do set "_DN_DIR=%%~dpd"
if "!_DN_DIR:~-1!"=="\" set "_DN_DIR=!_DN_DIR:~0,-1!"

rem --- 4) carpeta de herramientas -----------------------------------------
rem  En el PC de desarrollo (D:\Programs) se mantiene el aislamiento completo
rem  bajo D:\Programs (cache de NuGet y home de dotnet fuera de C:). En
rem  cualquier otro equipo se respeta la configuracion del usuario.
if /i "!_DN_DIR!"=="%_DN_DEVROOT%" (
  set "_DN_TOOLS=D:\Programs\dotnet-tools"
  set "_DN_NUGET=D:\Programs\nuget-packages"
  set "_DN_CLIHOME=D:\Programs\dotnet-home"
) else (
  set "_DN_TOOLS=%_DN_REPO%\.dotnet-tools"
  set "_DN_NUGET=!_DN_NUGET_IN!"
  set "_DN_CLIHOME=!_DN_CLIHOME_IN!"
)

echo   [dotnet] SDK en uso:  "%_DN_FOUND%"
pushd "%_DN_REPO%"
"%_DN_FOUND%" --version
popd

rem  Exportar al script que llamo. Se usa una cadena de  & set  (no un bloque
rem  entre parentesis) porque PATH suele contener  ")"  de  "Program Files
rem  (x86)"  y cerraria el bloque antes de tiempo.
endlocal & set "DOTNET=%_DN_FOUND%" & set "DOTNET_ROOT=%_DN_DIR%" & set "DOTNET_TOOLS=%_DN_TOOLS%" & set "NUGET_PACKAGES=%_DN_NUGET%" & set "DOTNET_CLI_HOME=%_DN_CLIHOME%" & set "DOTNET_CLI_TELEMETRY_OPTOUT=1" & set "DOTNET_NOLOGO=1" & set "DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1" & set "PATH=%_DN_DIR%;%_DN_TOOLS%;%PATH%"
exit /b 0
