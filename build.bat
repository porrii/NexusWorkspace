@echo off
setlocal EnableExtensions EnableDelayedExpansion
title NexusWorkspace build

rem ============================================================================
rem  NexusWorkspace - script de compilacion reutilizable
rem
rem  Uso:  build.bat [debug^|release] [run] [clean] [notest]
rem
rem    build.bat            restore + build Debug + test
rem    build.bat release    lo mismo en Release
rem    build.bat run        ademas lanza la app de escritorio al terminar
rem    build.bat clean      borra bin/obj y limpia antes de compilar
rem    build.bat notest     no ejecuta dotnet test
rem    (se pueden combinar:  build.bat release run)
rem
rem  Que hace:
rem   1. Comprueba si hay .NET 9 SDK. Si no, lo instala (sin admin) en
rem      D:\Archivos de programa\dotnet   (xcopy, portable).
rem   2. Comprueba/instala la herramienta dotnet-ef en
rem      D:\Archivos de programa\dotnet-tools
rem   3. dotnet restore
rem   4. Crea la migracion EF 'Initial' SOLO si aun no existe ninguna.
rem   5. dotnet build   6. dotnet test
rem   7. Guarda todos los logs en  build-logs\<fecha_hora>\  con un SUMMARY.txt
rem      listo para enviar.
rem ============================================================================

rem ---- Configuracion (editable) ---------------------------------------------
set "TOOLS_BASE=D:\Archivos de Programa"
set "DOTNET_ROOT=%TOOLS_BASE%\dotnet"
set "DOTNET_TOOLS=%TOOLS_BASE%\dotnet-tools"
set "DOTNET_CHANNEL=9.0"
set "EF_VERSION=9.*"
rem  Descomenta para tener tambien la cache de NuGet en D:
rem set "NUGET_PACKAGES=%TOOLS_BASE%\nuget-packages"

rem ---- Interno -------------------------------------------------------------
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "DOTNET_NOLOGO=1"
set "DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1"

cd /d "%~dp0"
set "REPO=%CD%"
set "SLN=NexusWorkspace.sln"
set "INFRA=src\NexusWorkspace.Infrastructure\NexusWorkspace.Infrastructure.csproj"
set "DESKTOP=src\NexusWorkspace.Desktop\NexusWorkspace.Desktop.csproj"
set "MIG_DIR=src\NexusWorkspace.Infrastructure\Persistence\Migrations"

set "CONFIG=Debug"
set "DO_RUN=0"
set "DO_CLEAN=0"
set "DO_TEST=1"

:parseargs
if "%~1"=="" goto endargs
if /i "%~1"=="debug"   set "CONFIG=Debug"
if /i "%~1"=="release" set "CONFIG=Release"
if /i "%~1"=="run"     set "DO_RUN=1"
if /i "%~1"=="clean"   set "DO_CLEAN=1"
if /i "%~1"=="notest"  set "DO_TEST=0"
if /i "%~1"=="help"    goto usage
if /i "%~1"=="-h"      goto usage
if /i "%~1"=="/?"      goto usage
shift
goto parseargs
:endargs

for /f "usebackq delims=" %%t in (`powershell -NoProfile -Command "Get-Date -Format yyyy-MM-dd_HHmmss"`) do set "STAMP=%%t"
set "LOGDIR=%REPO%\build-logs\%STAMP%"
mkdir "%LOGDIR%" 2>nul
set "INSTALLLOG=%LOGDIR%\00-install.log"
set "ENVLOG=%LOGDIR%\01-environment.log"
set "RESTORELOG=%LOGDIR%\02-restore.log"
set "MIGLOG=%LOGDIR%\03-migration.log"
set "BUILDLOG=%LOGDIR%\04-build.log"
set "TESTLOG=%LOGDIR%\05-test.log"
set "SUMMARY=%LOGDIR%\SUMMARY.txt"

echo.
echo ==== NexusWorkspace build ============================================
echo   Repo:    %REPO%
echo   Config:  %CONFIG%    run=%DO_RUN%  clean=%DO_CLEAN%  test=%DO_TEST%
echo   Logs:    %LOGDIR%
echo =====================================================================
echo.

set "STEP_ENV=PENDIENTE"
set "STEP_RESTORE=PENDIENTE"
set "STEP_MIG=PENDIENTE"
set "STEP_BUILD=PENDIENTE"
set "STEP_TEST=PENDIENTE"
set "SDK_STATE=?"
set "EF_STATE=?"

rem =====================================================================
rem  1) .NET SDK 9
rem =====================================================================
echo [1/5] Comprobando .NET %DOTNET_CHANNEL% SDK...

if exist "%DOTNET_ROOT%\dotnet.exe" set "PATH=%DOTNET_ROOT%;%PATH%"

call :haveSdk9
if "!HAVE_SDK9!"=="1" (
    echo        .NET %DOTNET_CHANNEL% ya disponible.
    set "SDK_STATE=ya presente"
) else (
    echo        No hay .NET %DOTNET_CHANNEL%. Instalando en:
    echo        "%DOTNET_ROOT%"
    if not exist "%TOOLS_BASE%" mkdir "%TOOLS_BASE%" 2>nul
    powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; [Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1' -OutFile ($env:TEMP + '\dotnet-install.ps1'); & ($env:TEMP + '\dotnet-install.ps1') -Channel '%DOTNET_CHANNEL%' -InstallDir '%DOTNET_ROOT%' -NoPath" 1>"%INSTALLLOG%" 2>&1
    set "PATH=%DOTNET_ROOT%;%PATH%"
    call :haveSdk9
    if not "!HAVE_SDK9!"=="1" (
        echo        ERROR: la instalacion de .NET fallo. Revisa "%INSTALLLOG%".
        set "STEP_ENV=FALLO (instalacion .NET)"
        set "SDK_STATE=fallo de instalacion"
        goto summary
    )
    echo        .NET instalado correctamente.
    set "SDK_STATE=instalado ahora"
)
if exist "%DOTNET_ROOT%\dotnet.exe" set "DOTNET_ROOT=%DOTNET_ROOT%"
set "STEP_ENV=OK"

rem =====================================================================
rem  2) herramienta dotnet-ef (para migraciones)
rem =====================================================================
if exist "%DOTNET_TOOLS%\dotnet-ef.exe" (
    set "PATH=%DOTNET_TOOLS%;%PATH%"
    set "EF_STATE=ya presente"
) else (
    echo        Instalando dotnet-ef en "%DOTNET_TOOLS%" ...
    dotnet tool install dotnet-ef --version %EF_VERSION% --tool-path "%DOTNET_TOOLS%" 1>>"%INSTALLLOG%" 2>&1
    if exist "%DOTNET_TOOLS%\dotnet-ef.exe" (
        set "PATH=%DOTNET_TOOLS%;%PATH%"
        set "EF_STATE=instalado ahora"
    ) else (
        set "EF_STATE=no disponible ^(se usara EnsureCreated^)"
    )
)

echo === dotnet --info === > "%ENVLOG%"
dotnet --info >> "%ENVLOG%" 2>&1
echo. >> "%ENVLOG%"
echo === dotnet --list-sdks === >> "%ENVLOG%"
dotnet --list-sdks >> "%ENVLOG%" 2>&1
echo. >> "%ENVLOG%"
echo === where === >> "%ENVLOG%"
where dotnet >> "%ENVLOG%" 2>&1
where dotnet-ef >> "%ENVLOG%" 2>&1
echo. >> "%ENVLOG%"
echo === git === >> "%ENVLOG%"
git rev-parse --abbrev-ref HEAD >> "%ENVLOG%" 2>&1
git log -1 --oneline >> "%ENVLOG%" 2>&1
git status --porcelain >> "%ENVLOG%" 2>&1

rem =====================================================================
rem  3) clean (opcional)
rem =====================================================================
if "%DO_CLEAN%"=="1" (
    echo [clean] Limpiando bin/obj ...
    dotnet clean "%SLN%" -c %CONFIG% 1>"%LOGDIR%\clean.log" 2>&1
    for /d /r %%d in (bin obj) do if exist "%%d" rd /s /q "%%d" 2>nul
)

rem =====================================================================
rem  4) restore
rem =====================================================================
echo [2/5] dotnet restore ...
powershell -NoProfile -ExecutionPolicy Bypass -Command "dotnet restore '%SLN%' 2>&1 | Tee-Object -FilePath '%RESTORELOG%'; exit $LASTEXITCODE"
if errorlevel 1 (
    set "STEP_RESTORE=FALLO"
    goto summary
)
set "STEP_RESTORE=OK"

rem =====================================================================
rem  5) migracion EF 'Initial' (solo si no existe ninguna)
rem =====================================================================
dir /b "%MIG_DIR%\*.cs" >nul 2>nul
if errorlevel 1 (
    where dotnet-ef >nul 2>nul
    if errorlevel 1 (
        echo [3/5] migracion: OMITIDA ^(dotnet-ef no disponible; la app usara EnsureCreated^)
        set "STEP_MIG=OMITIDO (sin dotnet-ef)"
    ) else (
        echo [3/5] Creando migracion EF 'Initial' ...
        powershell -NoProfile -ExecutionPolicy Bypass -Command "dotnet ef migrations add Initial --project '%INFRA%' --startup-project '%DESKTOP%' --output-dir Persistence/Migrations 2>&1 | Tee-Object -FilePath '%MIGLOG%'; exit $LASTEXITCODE"
        if errorlevel 1 (
            set "STEP_MIG=FALLO"
        ) else (
            set "STEP_MIG=OK (creada)"
        )
    )
) else (
    echo [3/5] migracion: OMITIDA ^(ya existe^)
    set "STEP_MIG=OMITIDO (ya existe)"
)

rem =====================================================================
rem  6) build
rem =====================================================================
echo [4/5] dotnet build -c %CONFIG% ...
powershell -NoProfile -ExecutionPolicy Bypass -Command "dotnet build '%SLN%' -c %CONFIG% --no-restore 2>&1 | Tee-Object -FilePath '%BUILDLOG%'; exit $LASTEXITCODE"
if errorlevel 1 (
    set "STEP_BUILD=FALLO"
    goto summary
)
set "STEP_BUILD=OK"

rem =====================================================================
rem  7) test
rem =====================================================================
if "%DO_TEST%"=="1" (
    echo [5/5] dotnet test ...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "dotnet test '%SLN%' -c %CONFIG% --no-build --logger 'trx;LogFileName=results.trx' 2>&1 | Tee-Object -FilePath '%TESTLOG%'; exit $LASTEXITCODE"
    if errorlevel 1 (
        set "STEP_TEST=FALLO"
    ) else (
        set "STEP_TEST=OK"
    )
) else (
    echo [5/5] test: OMITIDO ^(notest^)
    set "STEP_TEST=OMITIDO"
)

goto summary

rem =====================================================================
:summary
set "ERRCOUNT=0"
set "WARNCOUNT=0"
for /f %%c in ('powershell -NoProfile -Command "if(Test-Path '%BUILDLOG%'){(Select-String -Path '%BUILDLOG%' -Pattern ': error ' -SimpleMatch -AllMatches).Count}else{0}"') do set "ERRCOUNT=%%c"
for /f %%c in ('powershell -NoProfile -Command "if(Test-Path '%BUILDLOG%'){(Select-String -Path '%BUILDLOG%' -Pattern ': warning ' -SimpleMatch -AllMatches).Count}else{0}"') do set "WARNCOUNT=%%c"

> "%SUMMARY%" echo NexusWorkspace build  -  %STAMP%
>>"%SUMMARY%" echo Repo:    %REPO%
>>"%SUMMARY%" echo Config:  %CONFIG%
>>"%SUMMARY%" echo.
>>"%SUMMARY%" echo .NET SDK 9  : %SDK_STATE%   ("%DOTNET_ROOT%")
>>"%SUMMARY%" echo dotnet-ef   : %EF_STATE%
>>"%SUMMARY%" echo.
>>"%SUMMARY%" echo   [1] entorno ....... %STEP_ENV%
>>"%SUMMARY%" echo   [2] restore ....... %STEP_RESTORE%
>>"%SUMMARY%" echo   [3] migracion ..... %STEP_MIG%
>>"%SUMMARY%" echo   [4] build ......... %STEP_BUILD%   (errores: %ERRCOUNT%  avisos: %WARNCOUNT%)
>>"%SUMMARY%" echo   [5] test .......... %STEP_TEST%
>>"%SUMMARY%" echo.
git log -1 --oneline >> "%SUMMARY%" 2>&1
>>"%SUMMARY%" echo.

if exist "%BUILDLOG%" (
    >>"%SUMMARY%" echo --- errores de build (hasta 100) ------------------------------------
    powershell -NoProfile -Command "if(Test-Path '%BUILDLOG%'){ Select-String -Path '%BUILDLOG%' -Pattern ': error ' -SimpleMatch | Select-Object -First 100 -ExpandProperty Line }" >> "%SUMMARY%" 2>nul
    >>"%SUMMARY%" echo.
    >>"%SUMMARY%" echo --- avisos de build (hasta 60) ------------------------------------
    powershell -NoProfile -Command "if(Test-Path '%BUILDLOG%'){ Select-String -Path '%BUILDLOG%' -Pattern ': warning ' -SimpleMatch | Select-Object -First 60 -ExpandProperty Line }" >> "%SUMMARY%" 2>nul
    >>"%SUMMARY%" echo.
)
if exist "%TESTLOG%" (
    >>"%SUMMARY%" echo --- resumen de test --------------------------------------------------
    powershell -NoProfile -Command "if(Test-Path '%TESTLOG%'){ Select-String -Path '%TESTLOG%' -Pattern 'Passed!','Failed!','Passed:','Failed:','Skipped:','\[FAIL\]','error CS' | Select-Object -First 80 -ExpandProperty Line }" >> "%SUMMARY%" 2>nul
    >>"%SUMMARY%" echo.
)
>>"%SUMMARY%" echo Logs completos: %LOGDIR%

echo.
echo =====================================================================
type "%SUMMARY%"
echo =====================================================================
echo.
echo Logs en:  %LOGDIR%
echo Enviame la carpeta completa, o al menos SUMMARY.txt + los .log con FALLO.
echo.

if "%DO_RUN%"=="1" if "%STEP_BUILD%"=="OK" (
    echo Lanzando NexusWorkspace...
    dotnet run --project "%DESKTOP%" -c %CONFIG% --no-build
)

endlocal
exit /b 0

rem =====================================================================
:haveSdk9
set "HAVE_SDK9=0"
where dotnet >nul 2>nul || goto :eof
for /f "usebackq tokens=1" %%s in (`dotnet --list-sdks 2^>nul`) do (
    echo %%s | findstr /b /c:"%DOTNET_CHANNEL%." >nul && set "HAVE_SDK9=1"
)
goto :eof

:usage
echo.
echo Uso: build.bat [debug^|release] [run] [clean] [notest]
echo.
echo   (sin args)   restore + build Debug + test
echo   release      compila en Release
echo   run          lanza la app al terminar
echo   clean        borra bin/obj antes
echo   notest       no ejecuta los tests
echo.
endlocal
exit /b 0
