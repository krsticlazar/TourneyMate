@echo off
title Pokretanje TourneyMate projekta

:: Provera administratorskih prava
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo Ova skripta mora biti pokrenuta kao administrator!
    echo Desnim klikom na fajl izaberi "Run as administrator"
    pause
    exit /b 1
)

:: Postavi root folder (gde se nalazi skripta) kao trenutni
cd /d "%~dp0"
echo Pokrecem servere iz: %cd%

:: Prvi cmd - API
start "TourneyMate API" cmd /k "cd /d src/TourneyMate.Api && dotnet run"

:: Drugi cmd - Web
start "TourneyMate Web" cmd /k "cd /d src/TourneyMate.Web && npm run dev"

:: Kratko sačekaj da se prozori pokrenu
timeout /t 2 /nobreak >nul

:: Zatvori glavnu administratorsku konzolu
exit