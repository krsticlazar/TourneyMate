@echo off
setlocal EnableExtensions

set "SCRIPT_DIR=%~dp0"

pushd "%SCRIPT_DIR%"
docker compose -f "docker-compose.yml" up -d
popd

echo Seeding Neo4j...
docker exec -i tourneymate_neo4j cypher-shell -u neo4j -p trstenik < "%SCRIPT_DIR%seed_neo4j.cypher"
if errorlevel 1 (
  echo [ERROR] Neo4j seed failed.
  pause
  exit /b 1
)

echo Seeding Redis...
docker exec -i tourneymate_redis_6380 redis-cli -n 0 < "%SCRIPT_DIR%seed_redis.redis"
if errorlevel 1 (
  echo [ERROR] Redis seed failed.
  pause
  exit /b 1
)

echo.
echo [OK] Reset + seed finished.
echo.
pause
endlocal
