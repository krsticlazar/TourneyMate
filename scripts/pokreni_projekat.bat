@echo off
title Pokretanje TourneyMate projekta
setlocal EnableExtensions
set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "ROOT_DIR=%%~fI"

:: Provera administratorskih prava
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo Ova skripta mora biti pokrenuta kao administrator!
    echo Desnim klikom na fajl izaberi "Run as administrator"
    pause
    exit /b 1
)

if not exist "%ROOT_DIR%\src\TourneyMate.Api" (
    echo [ERROR] Putanja nije pronadjena: "%ROOT_DIR%\src\TourneyMate.Api"
    pause
    exit /b 1
)

if not exist "%ROOT_DIR%\src\TourneyMate.Web" (
    echo [ERROR] Putanja nije pronadjena: "%ROOT_DIR%\src\TourneyMate.Web"
    pause
    exit /b 1
)

echo Pokrecem servere iz: %ROOT_DIR%

:: Prvi cmd - API
start "TourneyMate API" cmd /k "cd /d ""%ROOT_DIR%\src\TourneyMate.Api"" && dotnet run"

:: Drugi cmd - Web
start "TourneyMate Web" cmd /k "cd /d ""%ROOT_DIR%\src\TourneyMate.Web"" && npm run dev"

:: Kratko sacekaj da se prozori pokrenu
timeout /t 2 /nobreak >nul

:: Zatvori glavnu administratorsku konzolu
exit
