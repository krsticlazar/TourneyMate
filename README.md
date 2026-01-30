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
* **SignalR** – Real-time komunikacija za chat

### Frontend
* **React + TypeScript** – Klijentska aplikacija
* **Vite** – Build tool
* **Tailwind CSS** – Styling
* **Socket.io client** – Real-time chat

### DevOps
* **Docker** – Containerizacija baza
* **Docker Compose** – Orchestration

---

## 🚀 Pokretanje aplikacije (lokalni razvoj)

### Preduslovi
* **Docker Desktop**
* **.NET 8 SDK**
* **Node.js 18+**
* **Git**