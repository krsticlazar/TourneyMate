# TourneyMate

## Opis
TourneyMate je web aplikacija za pracenje i upravljanje sportskim turnirima.

## Tehnologije
- Backend: ASP.NET Core Web API (.NET)
- Frontend: React + TypeScript
- Baze: Neo4j + Redis
- Kontejneri: Docker + Docker Compose
- Testiranje:
  - NUnit (komponentni/backend testovi)
  - Microsoft Playwright for .NET (E2E i API testovi)

## Pokretanje projekta

1. Pokreni baze preko Docker-a i ucitaj testne podatke:
```bash
scripts\pokreni_docker.cmd
```

2. Pokreni backend:
```bash
cd src\TourneyMate.Api
dotnet run
```

3. Pokreni frontend:
```bash
cd src\TourneyMate.Web
npm install
npm run dev
```

4. Nakon svakog sledeceg pokretanja preskocite prva 3 koraka:
```bash
scripts\pokreni_projekat.bat   --Run as administrator
```

## Pokretanje testova

1. Backend testovi (NUnit):
```bash
dotnet test src\Api.Tests\Api.Tests.csproj --nologo
```

2. Frontend E2E/API testovi (Playwright):
```bash
dotnet test src\Web.Tests\Web.Tests.csproj --nologo
```

3. Svi testovi odjednom:
```bash
dotnet test TourneyMate.sln --nologo
```

4. Ako Playwright browser-i nisu instalirani:
```bash
powershell -ExecutionPolicy Bypass -File src\Web.Tests\bin\Debug\net10.0\playwright.ps1 install
```
