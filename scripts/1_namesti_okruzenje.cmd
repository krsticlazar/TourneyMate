@echo off
title Namestanje TourneyMate okruzenja
setlocal EnableExtensions

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "ROOT_DIR=%%~fI"
set "SOLUTION_PATH=%ROOT_DIR%\src\src.sln"
set "WEB_DIR=%ROOT_DIR%\src\TourneyMate.Web"

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo Ova skripta mora biti pokrenuta kao administrator.
    echo Desni klik na fajl i izaberi "Run as administrator".
    pause
    exit /b 1
)

where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] dotnet nije pronadjen u PATH-u.
    echo Instaliraj .NET SDK i pokreni ponovo.
    pause
    exit /b 1
)

where npm >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] npm nije pronadjen u PATH-u.
    echo Instaliraj Node.js LTS i pokreni ponovo.
    pause
    exit /b 1
)

if not exist "%SOLUTION_PATH%" (
    echo [ERROR] Nije pronadjena solution putanja: "%SOLUTION_PATH%"
    pause
    exit /b 1
)

if not exist "%WEB_DIR%\package.json" (
    echo [ERROR] Nije pronadjen frontend package.json: "%WEB_DIR%\package.json"
    pause
    exit /b 1
)

echo [1/3] dotnet restore
pushd "%ROOT_DIR%\src"
dotnet restore "src.sln" --nologo
if errorlevel 1 (
    popd
    echo [ERROR] dotnet restore nije uspeo.
    pause
    exit /b 1
)
popd
echo [OK] .NET paketi su instalirani.
echo.

echo [2/3] dotnet build
pushd "%ROOT_DIR%\src"
dotnet build "src.sln" --no-restore --nologo
if errorlevel 1 (
    popd
    echo [ERROR] dotnet build nije uspeo.
    pause
    exit /b 1
)
popd
echo [OK] .NET projekti su uspesno build-ovani.
echo.

echo [3/3] npm install
pushd "%WEB_DIR%"
if exist "package-lock.json" (
    call npm ci
) else (
    call npm install
)
if errorlevel 1 (
    popd
    echo [ERROR] npm instalacija nije uspela.
    pause
    exit /b 1
)
popd
echo [OK] Frontend paketi su instalirani.
echo.

echo ========================================
echo [OK] Okruzenje je spremno.
echo ========================================
echo.
pause
endlocal
