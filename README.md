# TourneyMate - Projektni zadatak

## 📌 Opis projekta
**TourneyMate** je web aplikacija za praćenje lokalnih sportskih turnira u real-time režimu. Sistem omogućava posetiocima da prate turnire, čitaju chat i gledaju leaderboard, dok registrovani korisnici mogu da se prijavljuju na turnire, kreiraju timove i komuniciraju. Hostovi turnira upravljaju prijavama i celokupnim tokom turnira.

---

## 👥 Autor
**Student:** Lazar Krstić, 19190

---

## 🏗️ Tehnologije

### Backend
* **.NET 8** – API aplikacija
* **Entity Framework Core** – ORM
* **Neo4j** – Graph baza za relacione podatke (timovi, turniri, prijave)
* **Redis** – Za real-time chat i caching

### Frontend
* **React + TypeScript** – Klijentska aplikacija

### DevOps
* **Docker** – Containerizacija baza
* **Docker Compose** – Orchestration
* **Redis Insight** - Pogled na redis
* **Neo4j Browser** - Pogled na Neo4j

---

## 🚀 Pokretanje aplikacije (lokalni razvoj)

### Preduslovi
* **Git** **[git clone]**
* **Docker Desktop**    **[cd scripts && reset_and_seed.cmd]**
* **.NET 8 SDK**        **[cd src\TourneyMate.Api && dotnet build && dotnet run]**
* **Node.js 18+**       **[cd src\TourneyMate.Web && npm install && npm run dev]**
