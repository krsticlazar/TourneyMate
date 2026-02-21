@echo off
setlocal EnableExtensions

echo Pokrecem Docker kontejnere i seedovanje...
echo.

REM Uvek radi relativno na lokaciju ove skripte (scripts folder)
set "SCRIPT_DIR=%~dp0"
set "ROOT_DIR=%SCRIPT_DIR%"

REM 1) Pokreni Docker Compose (yml je u istom folderu)
echo [1/3] Pokrecem docker compose...
docker compose -f "%ROOT_DIR%docker-compose.yml" up -d
if errorlevel 1 (
  echo [ERROR] Docker compose nije uspeo!
  pause
  exit /b 1
)
echo ✅ Docker compose pokrenut
echo.

REM Sacekaj da se Neo4j potpuno pokrene (treba mu ~20s)
echo Cekam da se Neo4j pokrene (25s)...
timeout /t 25 /nobreak >nul

REM 2) Seed Neo4j
echo [2/3] Seeding Neo4j...
docker exec -i tourneymate_neo4j cypher-shell -u neo4j -p trstenik < "%ROOT_DIR%seed_neo4j.cypher"
if errorlevel 1 (
  echo [ERROR] Neo4j seed failed!
  pause
  exit /b 1
)
echo ✅ Neo4j seedovan
echo.

REM 3) Seed Redis
echo [3/3] Seeding Redis...
docker exec -i tourneymate_redis redis-cli -n 0 < "%ROOT_DIR%seed_redis.redis"
if errorlevel 1 (
  echo [ERROR] Redis seed failed!
  pause
  exit /b 1
)
echo ✅ Redis seedovan
echo.

echo ========================================
echo ✅ RESET + SEED ZAVRSEN USPESNO!
echo ========================================
echo.
echo Kontejneri su pokrenuti i popunjeni.
echo.
pause
endlocal