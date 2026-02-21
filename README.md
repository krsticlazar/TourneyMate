# TourneyMate

## O projektu
TourneyMate je web aplikacija za pracenje i upravljanje sportskim turnirima.

Tehnologije koje su koriscene:
- Backend: ASP.NET Core Web API (.NET 10)
- Frontend: React + TypeScript (Vite)
- Baze: Neo4j + Redis
- Kontejneri: Docker + Docker Compose

## Preduslovi
Pre pokretanja potrebno je da budu instalirani:
- Docker Desktop (da je upaljen)
- .NET 10 SDK
- Node.js LTS (sa npm)

Sve skripte pokretati kao administrator (Run as administrator).

## Pokretanje projekta (prvi put)
1. Pokrenuti `scripts\1_namesti_okruzenje.cmd`
   - Skripta radi `dotnet restore`, zatim `dotnet build` za backend projekte, i npm instalaciju za frontend.

2. Pokrenuti `scripts\2_pokreni_docker.cmd`
   - Skripta podize Neo4j i Redis kontejnere i ubacuje seed podatke.

3. Pokrenuti `scripts\3_pokreni_projekat.bat`
   - Skripta otvara dva CMD prozora: jedan za backend, jedan za frontend.

## Adrese
- Frontend: `http://localhost:5173`
- API: `http://localhost:5125`
- Swagger: `http://localhost:5125/swagger`
- Neo4j Browser: `http://localhost:7474`
  - username: `neo4j`
  - password: `trstenik`
- Neo4j Bolt: `localhost:7687`

Redis moze da se proverava kroz RedisInsight:
- Host: `127.0.0.1`
- Port: `6380`
- Username: prazno
- Password: prazno
- Database: `0`

## Sledeca pokretanja
Kada je projekat jednom podesen, za sledece pokretanje je u praksi dovoljno:
- `scripts\3_pokreni_projekat.bat`

Ako Docker kontejneri nisu aktivni, pre toga pokrenuti i:
- `scripts\2_pokreni_docker.cmd`

## Test podaci
Nakon seedovanja (pokretanje `scripts\2_pokreni_docker.cmd`) mogu da se koriste sledeci test nalozi:

- Viewer:
  - username: `viewer01`
  - password: `view123`
- Host:
  - username: `host01`
  - password: `host123`
- Admin:
  - username: `admin01`
  - password: `admin123`

## Testiranje i kvalitet softvera
Ovaj deo je vezan za predmet Testiranje i kvalitet softvera.

Za testove je potrebno:
- .NET 10 SDK
- VS Code sa C# ekstenzijom (po zelji i Test Explorer UI)

Pokretanje testova iz VS Code:
1. Otvoriti repo u VS Code.
2. Otvoriti Testing panel.
3. Pokrenuti API testove iz `src\Api.Tests`.
4. Pokrenuti Web testove iz `src\Web.Tests`.


## Napomena
Projekat je uradjen kao obaveza na predmetima Napredne baze podataka i Testiranje i kvalitet softvera.
