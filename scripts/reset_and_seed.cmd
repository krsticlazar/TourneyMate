@echo off
setlocal EnableExtensions

REM Uvek radi relativno na lokaciju ove skripte
set "SCRIPT_DIR=%~dp0"
set "ROOT_DIR=%SCRIPT_DIR%.."

REM 1) Redis + Neo4j moraju da budu upaljeni (compose je u root-u)
pushd "%ROOT_DIR%"
docker compose up -d
popd

REM 2) Seed Neo4j
echo Seeding Neo4j...
docker exec -i tourneymate_neo4j cypher-shell -u neo4j -p trstenik < "%SCRIPT_DIR%seed_neo4j.cypher"
if errorlevel 1 (
  echo [ERROR] Neo4j seed failed.
  pause
  exit /b 1
)

REM 3) Seed Redis
echo Seeding Redis...
docker exec -i tourneymate_redis_6380 redis-cli -n 0 < "%SCRIPT_DIR%seed_redis.redis"
if errorlevel 1 (
  echo [ERROR] Redis seed failed.
  pause
  exit /b 1
)

echo.
echo ✅ Reset + seed finished.
echo.
pause
endlocal
