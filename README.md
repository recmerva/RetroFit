# RetroFit - marketplace za polovne stvari

Lokalni marketplace za **kupovinu, prodaju, razmenu i poklanjanje** polovnih stvari. Projekat iz predmeta **Napredne baze podataka**.

Korisnici postavljaju oglase sa slikama i atributima po kategoriji, pretražuju (Full-Text, filteri, udaljenost), dopisuju se, pregovaraju o ceni (ponude i kontraponude), rezervišu, kupuju uz mock escrow, ocenjuju jedni druge, čuvaju omiljene oglase i pretrage, objavljuju zahteve "Tražim..." i prijavljuju sumnjive oglase. Moderatori obrađuju prijave i blokiraju naloge, a administratori upravljaju kategorijama, ulogama, izdvojenim oglasima, uređajima i vide statistiku i audit log. Sopstveni **Bot Detection / Fingerprinting middleware** štiti od botova, scraping-a, masovnog pravljenja naloga i spama.

## Tehnologije

| Sloj | Tehnologije |
|---|---|
| Backend | .NET 8, ASP.NET Core Web API, EF Core 8 (Code First, migracije), SQL Server 2022 (Full-Text Search, `geography`), MediatR (CQRS), AutoMapper, FluentValidation, JWT + refresh tokeni sa rotacijom, Serilog, Swagger/OpenAPI, API verzionisanje |
| Frontend | React 19, TypeScript, Vite, React Router, Axios, Tailwind CSS |
| Testovi | xUnit, Moq, FluentAssertions - **162 testa** |
| DevOps | Docker, Docker Compose, GitHub Actions |

## Dokumentacija

- [Arhitektura](docs/arhitektura.md) - Clean Architecture, CQRS tok, šabloni, domenski model
- [Baza podataka](docs/baza-podataka.md) - odluke o modelu, ograničenja, indeksi, FTS, spatial, CTE, konkurentnost
- [ER dijagram](docs/ER-dijagram.md) - sve tabele sa atributima i vezama
- [Bot Detection middleware](docs/bot-detekcija.md) - signali, odluke, JS izazov, privatnost, ograničenja
- [Bezbednost](docs/bezbednost.md) - autentifikacija, autorizacija, privatnost, zavisnosti
- [API](docs/api.md) - endpointi, format grešaka, primeri (interaktivno: Swagger)
- [Priprema za odbranu](docs/odbrana.md) - gde se šta nalazi, pitanja i odgovori, scenario demonstracije

## Struktura

```
├─ backend/                     BACKEND (.NET 8)
│  ├─ RetroFit.API/             kontroleri, middleware (Bot Detection), konfiguracija - ovo se pokreće
│  ├─ RetroFit.Application/     CQRS komande/upiti, DTO, validatori, interfejsi
│  ├─ RetroFit.Domain/          entiteti i poslovna pravila (bez zavisnosti)
│  ├─ RetroFit.Infrastructure/  EF Core, migracije, repozitorijumi, UoW, servisi, poslovi
│  ├─ RetroFit.Tests/           jedinični testovi
│  ├─ RetroFit.sln              solution (svi .NET projekti)
│  └─ global.json, Directory.*.props   verzija SDK-a i zajedničke postavke/paketi
├─ frontend/                    FRONTEND (React aplikacija)
├─ docker/sqlserver-fts/        SQL Server image sa Full-Text Search
├─ docs/                        dokumentacija
├─ docker-compose.yml
└─ .github/workflows/ci.yml
```

## Pokretanje - Docker (preporučeno)

Potrebno: Docker Desktop.

```bash
cp .env.example .env        # opciono: promenite lozinke i ključeve
docker compose up --build
```

- Aplikacija: **http://localhost:8080**
- Swagger: **http://localhost:5000/swagger**
- SQL Server: `localhost,14333` (korisnik `sa`, lozinka iz `.env`)

Pri prvom pokretanju API sam primenjuje migracije (uključujući Full-Text i prostorni indeks) i puni demo podatke. Podaci baze i otpremljene slike čuvaju se u Docker volume-ima `sqldata` i `uploads`.

## Pokretanje - lokalno (razvoj)

Potrebno: .NET SDK 8.0.403+, Node.js 22, SQL Server 2022 (Developer/Express; za pravi FTS komponenta *Full-Text and Semantic Extractions for Search*).

```bash
# 1) Backend (primenjuje migracije i demo podatke u Development okruženju)
cd backend/RetroFit.API
dotnet run --launch-profile http          # http://localhost:5029/swagger

# 2) Frontend (u drugom terminalu)
cd frontend
npm install
npm run dev                                # http://localhost:5173
```

Aplikacija se otvara na **http://localhost:5173**; pozivi ka `/api` idu kroz Vite proxy do backenda, pa nije potrebno posebno CORS podešavanje.

Konekcioni string je u `backend/RetroFit.API/appsettings.json` (podrazumevano `Server=.` sa Windows autentifikacijom).
Ručno upravljanje migracijama:

```bash
cd backend
dotnet tool restore
dotnet ef database update --project RetroFit.Infrastructure --startup-project RetroFit.Infrastructure
dotnet ef migrations add NazivMigracije --project RetroFit.Infrastructure --startup-project RetroFit.Infrastructure --output-dir Persistencija/Migracije
```

Ako SQL Server nema Full-Text Search, pretraga automatski koristi `LIKE` (u logu: *"Pretraga oglasa koristi LIKE fallback"*); kada se komponenta doinstalira, dovoljno je ponovo primeniti migracije na novu bazu ili ručno pokrenuti SQL iz migracije `FullTextIProstorniIndeks`.

## Demo nalozi

| Nalog | Lozinka | Uloga |
|---|---|---|
| admin@retrofit.rs | Admin123! | Admin + Moderator |
| moderator@retrofit.rs | Moderator123! | Moderator |
| marko@retrofit.rs, ana@retrofit.rs, jovan@retrofit.rs, milica@retrofit.rs | Lozinka123 | Korisnik |

Mock SMS kod za verifikaciju telefona se u razvoju prikazuje na stranici profila (i u logu API-ja). Plaćanja su simulirana (mock escrow), bez unosa kartica.

## Testovi

```bash
cd backend
dotnet test
```

Pokrivaju: mašine stanja i invarijante entiteta (oglas, ponuda, rezervacija, transakcija, recenzija, korisnik, verifikacija, tokeni), FluentValidation validatore, handlere sa Moq-om (prijava, rotacija i ponovna upotreba refresh tokena, prihvatanje ponude, recenzije, escrow, privatnost telefona, ovlašćenja moderatora, pozadinski istek), Bot Detection (analizator rizika, odluke, klasifikacija ruta, klizni prozori, spam, robotski ritam, JS izazov, otisak, maskiranje IP-a) i infrastrukturu (Full-Text upit, prepoznavanje slika, JWT, heširanje lozinki).

## CI

GitHub Actions (`.github/workflows/ci.yml`) na svaki push: restore → build → testovi (sa artefaktom rezultata) → provera da migracije odgovaraju modelu; frontend lint + build; build svih Docker image-a.

## Poznata ograničenja

- Stanje Bot Detection-a i mock servisa (SMS, plaćanje) je u memoriji jedne instance API-ja; za horizontalno skaliranje potreban je Redis.
- TLS otisak (JA3/JA4) zahteva reverse proxy koji ga računa - vidi [bot-detekcija.md](docs/bot-detekcija.md).
- Obaveštenja i poruke se osvežavaju periodično (polling), bez WebSocket-a.
