# RetroFit - priprema za odbranu

Dokument ima četiri dela:
1. **Pre odbrane**: kako se projekat pokreće i šta treba proveriti.
2. **Mapa projekta**: gde se šta nalazi, sa putanjama i brojevima linija.
3. **Pitanja profesora sa odgovorima**: najviše pitanja o bazi, jer je predmet Napredne baze podataka.
4. **Scenario demonstracije**: redosled prikaza na odbrani.

> Putanje su relativne u odnosu na folder projekta `C:\Users\recme\OneDrive\Desktop\NBP 19`.
> Kratke putanje kao `Infrastructure/...`, `Application/...`, `Domain/...` ili `API/...` znače `backend/RetroFit.Infrastructure/...` itd.
> Brojevi linija važe za commit `263a575`. Ako se kod menja, pomeraju se za nekoliko redova.

---

## 1. Pre odbrane

### Pokretanje (dva terminala)

```powershell
# Terminal 1 - backend
cd "C:\Users\recme\OneDrive\Desktop\NBP 19\backend\RetroFit.API"
dotnet run --launch-profile http
# Swagger: http://localhost:5029/swagger

# Terminal 2 - frontend
cd "C:\Users\recme\OneDrive\Desktop\NBP 19\frontend"
npm run dev
# Sajt: http://localhost:5173
```

Za prikaz Bot Detection-a u punom režimu, backend se pokreće ovako:

```powershell
dotnet run --launch-profile http -- --BotDetekcija:Rezim=Zastita
```

### Checklista (veče pre odbrane)

- [ ] SQL Server servis radi: u `services.msc` je `SQL Server (MSSQLSERVER)` u stanju *Running*.
- [ ] `dotnet test` u folderu `backend` prolazi: **162 testa**.
- [ ] Backend se pokreće bez greške i Swagger se otvara.
- [ ] Frontend se pokreće i prijava sa `marko@retrofit.rs` radi.
- [ ] SSMS se povezuje na server `.` i baza `RetroFit` je vidljiva.
- [ ] Otvoreni tabovi: Swagger, sajt, SSMS sa pripremljenim upitima (deo 3.11), VS Code sa projektom.

### Demo nalozi

| Email | Lozinka | Uloga |
|---|---|---|
| `admin@retrofit.rs` | `Admin123!` | Admin + Moderator |
| `moderator@retrofit.rs` | `Moderator123!` | Moderator |
| `marko@retrofit.rs`, `ana@retrofit.rs`, `jovan@retrofit.rs`, `milica@retrofit.rs` | `Lozinka123` | Korisnik |

### Resetovanje baze (ako se podaci "isprljaju" pri vežbanju)

U SSMS-u (ili sqlcmd-u) izvršiti:

```sql
USE master;
ALTER DATABASE RetroFit SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE RetroFit;
```

Zatim ponovo pokrenuti backend. On sam primeni migracije i ubaci demo podatke (`PrimeniMigracije` i `DemoPodaci` su uključeni u `appsettings.Development.json`). Slike u `backend\RetroFit.API\uploads\oglasi` treba obrisati ručno.

---

## 2. Mapa projekta - gde se šta nalazi

### 2.1 Gde je baza

| Šta | Gde |
|---|---|
| Server | lokalni **SQL Server 2022 Developer**, podrazumevana instanca `.` (Windows autentifikacija) |
| Ime baze | `RetroFit` |
| Fajl sa podacima | `C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\RetroFit.mdf` |
| Transakcioni log | `C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\RetroFit_log.ldf` |
| Konekcioni string | `backend/RetroFit.API/appsettings.json:3` |
| Alat za pregled | SQL Server Management Studio 19 (Server name `.`, Windows Authentication) |
| Slike oglasa | na disku, ne u bazi: `backend/RetroFit.API/uploads/`. U bazi je samo putanja (`SlikeOglasa.Putanja`) |
| Logovi aplikacije | `backend/RetroFit.API/logs/retrofit-YYYYMMDD.log` (JSON, jedan fajl po danu) |

### 2.2 Slojevi (Clean Architecture)

| Projekat | Folder | Šta sadrži |
|---|---|---|
| Domain | `backend/RetroFit.Domain/` | entiteti i poslovna pravila, bez ijednog NuGet paketa |
| Application | `backend/RetroFit.Application/` | CQRS komande i upiti, handleri, DTO, validatori, interfejsi |
| Infrastructure | `backend/RetroFit.Infrastructure/` | EF Core, migracije, repozitorijumi, UoW, JWT, mock servisi, pozadinski poslovi |
| API | `backend/RetroFit.API/` | kontroleri, middleware (Bot Detection), Swagger, konfiguracija |
| Testovi | `backend/RetroFit.Tests/` | 162 testa (xUnit, Moq, FluentAssertions) |
| Frontend | `frontend/src/` | React aplikacija, 18 stranica |
| Dokumentacija | `docs/` | arhitektura, baza, ER dijagram, bezbednost, bot detekcija, API |

### 2.3 Baza podataka u kodu

| Tema | Fajl:linija |
|---|---|
| DbContext (sve tabele) | `backend/RetroFit.Infrastructure/Persistencija/RetroFitDbContext.cs` |
| Podrazumevano RESTRICT umesto CASCADE | `RetroFitDbContext.cs:56-63` |
| Svi datumi kao `datetime2(3)` + UTC konverter | `RetroFitDbContext.cs:68` (`ConfigureConventions`) |
| Konfiguracija tabela (ključevi, CHECK, indeksi) | `backend/RetroFit.Infrastructure/Persistencija/Konfiguracije/` |
| - korisnici, uloge, tokeni, blokade | `Konfiguracije/IdentitetKonfiguracije.cs` |
| - kategorije, gradovi, oglasi, slike, atributi | `Konfiguracije/KatalogKonfiguracije.cs` (oglasi od linije 89) |
| - razgovori, poruke, ponude, obaveštenja | `Konfiguracije/KomunikacijaKonfiguracije.cs` |
| - rezervacije, transakcije, plaćanja, recenzije | `Konfiguracije/TrgovinaKonfiguracije.cs` |
| - prijave, zahtevi "Tražim", uređaji, audit | `Konfiguracije/ModeracijaKonfiguracije.cs` |
| Eksplicitni CASCADE za "delove" entiteta | `Konfiguracije/KonfiguracijaPomoc.cs:34` |
| Migracija 1: šema | `Persistencija/Migracije/20261103124556_Inicijalna.cs` |
| Migracija 2: Full-Text + SPATIAL indeks (ručni SQL) | `Persistencija/Migracije/20261103124619_FullTextIProstorniIndeks.cs` (SPATIAL: 26, FTS katalog: 40, FTS indeksi: 43 i 48) |
| Migracija 3: svih 152 mesta Srbije + srpska kolacija za nazive gradova | `Persistencija/Migracije/20261106195636_SviGradoviSrbije.cs` |
| Seed u migraciji (uloge, gradovi, kategorije, atributi) | `Persistencija/Seed/PocetniPodaci.cs` |
| Demo podaci (korisnici, oglasi, kupovina) | `Persistencija/Seed/DemoPodaciSeeder.cs` |
| Automatsko primenjivanje migracija pri startu | `Persistencija/Seed/InicijalizacijaBaze.cs`, poziv u `backend/RetroFit.API/Program.cs:92` |

### 2.4 Napredne tehnike baze - gde su u kodu

| Tehnika | Fajl:linija |
|---|---|
| **Full-Text Search** (`CONTAINS`) | `Infrastructure/Persistencija/Repozitorijumi/OglasRepository.cs:201` (`Filtriraj`), komentar na 207 |
| Pravljenje bezbednog FTS upita | `Infrastructure/Pretraga/FullText.cs:63` (`FullTextUpit`) |
| Detekcija FTS + LIKE fallback | `Infrastructure/Pretraga/FullText.cs:20` (`FullTextDetektor`), SQL provera na 37 |
| **Prostorna pretraga** (`geography`, radijus) | `OglasRepository.cs:233` (`IsWithinDistance` → `STDistance`) |
| **Rekurzivni CTE** - preci (nasleđivanje atributa) | `Repozitorijumi/KatalogRepozitorijumi.cs:50-54` |
| **Rekurzivni CTE** - potomci (oglasi iz podkategorija) | `Repozitorijumi/KatalogRepozitorijumi.cs:72-75` |
| **Optimistička konkurentnost** (`rowversion`) | `KatalogKonfiguracije.cs:118`, `KomunikacijaKonfiguracije.cs:121`, `TrgovinaKonfiguracije.cs:24` i `68` |
| **Filtrirani UNIQUE indeksi** | `TrgovinaKonfiguracije.cs:28` (jedna aktivna rezervacija), `:74` (oglas se ne prodaje dvaput), `KomunikacijaKonfiguracije.cs:124` (jedna ponuda na čekanju), `ModeracijaKonfiguracije.cs:64` (jedna otvorena prijava) |
| **Atomični UPDATE** (prosek ocena) | `Repozitorijumi/KorisnikRepozitorijumi.cs:86-93` (`DodajOcenuAsync`) |
| Atomični UPDATE (broj pregleda) | `OglasRepository.cs:176` |
| Zamena redosleda slika jednom naredbom | `OglasRepository.cs:154` (`PromeniRedosledSlikaAsync`) |
| **Eksplicitne transakcije** | `Infrastructure/Persistencija/UnitOfWork.cs:112` (`UTransakcijiAsync`, execution strategy na 121) |
| Prevođenje SQL grešaka (2601/2627/547 → 409) | `UnitOfWork.cs:140` (`PrevediSqlGresku`), poruke po imenu indeksa od linije 31 |
| Konflikt `rowversion` → 409 | `UnitOfWork.cs:99` |
| **Audit preko EF interceptora** | `Infrastructure/Persistencija/Interceptori/AuditInterceptor.cs:23` |
| Filtrirani UNIQUE na telefonu | `IdentitetKonfiguracije.cs:38` |
| **EAV** (atributi po kategoriji) | entiteti `Domain/Entiteti/DefinicijaAtributa.cs` i `AtributOglasa.cs` |
| Denormalizacija (prosek ocena, brojači) | `Domain/Entiteti/Korisnik.cs:116` (`DodajOcenu`) + atomični UPDATE iznad |
| Meko brisanje | `Domain/Common/IMekoBrisanje.cs`, `Korisnik.cs:137` (`Obrisi` = anonimizacija) |

### 2.5 Ključni poslovni tokovi

| Tok | Fajl:linija |
|---|---|
| Registracija | `Application/Features/Autentifikacija/Komande.cs:33` |
| Prijava | `Autentifikacija/Komande.cs:98` |
| Osvežavanje tokena + detekcija krađe | `Autentifikacija/Komande.cs:155`; ponovna upotreba na 182; tolerancija od 30 s na 220 |
| Izdavanje JWT + refresh tokena | `Autentifikacija/IzdavanjeTokena.cs:21` |
| JWT potpis i validacija | `Infrastructure/Identitet/TokenServis.cs:31`, `API/Konfiguracija/ApiServisi.cs:170` |
| Heširanje lozinki (PBKDF2) | `Infrastructure/Servisi/OsnovniServisi.cs:16-23` |
| Kreiranje oglasa | `Application/Features/Oglasi/Komande.cs:109` |
| Pretraga oglasa | `Application/Features/Oglasi/Upiti.cs` → `OglasRepository.cs:86` |
| Privatnost telefona prodavca | `Oglasi/Upiti.cs:55` (`TelefonProdavcaAsync`), `Razgovori/Razgovori.cs:66` |
| **Prihvatanje ponude → rezervacija** | `Application/Features/Trgovina/Ponude.cs:90` (u transakciji od linije 96) |
| Kontraponuda | `Trgovina/Ponude.cs:166` |
| Potvrda transakcije + escrow | `Trgovina/Transakcije.cs:225` |
| Recenzija | `Trgovina/Transakcije.cs:342` |
| Mock plaćanje / mock SMS | `Infrastructure/Servisi/MockServisi.cs:44` / `:14` |
| Pozadinski poslovi (istek oglasa, ponuda, rezervacija) | `Infrastructure/PozadinskiPoslovi/PeriodicniPoslovi.cs:18` |

### 2.6 Domenski model (mašine stanja)

| Entitet | Fajl | Ključne metode |
|---|---|---|
| Oglas | `Domain/Entiteti/Oglas.cs` | `Objavi` (101), `Rezervisi` (109), `Ukloni` (154) |
| Ponuda | `Domain/Entiteti/Ponuda.cs` | `Prihvati` (76), `Kontraponuda` (104) |
| Rezervacija | `Domain/Entiteti/Rezervacija.cs` | `Kreiraj` (42) |
| Transakcija | `Domain/Entiteti/Transakcija.cs` | `IzRezervacije` (48), `Potvrdi` (94), `Otkazi` (121) |
| Recenzija | `Domain/Entiteti/Recenzija.cs` | `Kreiraj` (26) |
| Refresh token | `Domain/Entiteti/RefreshToken.cs` | `Opozovi`, `JeAktivan`, `JeNedavnoRotiran` |
| Poslovne konstante (60 dana, 48 h, 72 h, 10 slika...) | `Domain/Pravila/PoslovnaPravila.cs` | - |
| Reputacija ("pouzdan prodavac") | `Domain/Pravila/ReputacijaPravila.cs`, `Korisnik.cs:130` | - |

### 2.7 API sloj i šabloni

| Tema | Fajl:linija |
|---|---|
| Redosled middleware-a (pipeline) | `backend/RetroFit.API/Program.cs:37-90` |
| Registracija servisa (JSON, verzionisanje, CORS, JWT, politike) | `API/Konfiguracija/ApiServisi.cs` (MaxDepth 43, verzija 116, CORS 133, JWT 170, politike 191-192) |
| Bazni kontroler + `[ApiVersion]` | `API/Kontroleri/ApiKontroler.cs:13` |
| Kontroleri (12 fajlova, 100 endpointa) | `backend/RetroFit.API/Kontroleri/` |
| Globalni exception handler → ProblemDetails | `API/Middleware/GlobalniExceptionHandler.cs:16` |
| Korelacioni ID, bezbednosna zaglavlja | `API/Middleware/PomocniMiddleware.cs` |
| MediatR pipeline: validacija | `Application/Common/Ponasanja/Ponasanja.cs:12` |
| MediatR pipeline: logovanje | `Application/Common/Ponasanja/Ponasanja.cs:39` |
| AutoMapper profil | `Application/Common/Mapiranje/MapiranjeProfil.cs` |
| Interfejs Unit of Work | `Application/Common/Interfejsi/Persistencija/IUnitOfWork.cs` |
| Interfejsi repozitorijuma | `Application/Common/Interfejsi/Persistencija/Repozitorijumi.cs` |
| Generički repozitorijum | `Infrastructure/Persistencija/Repozitorijumi/Repository.cs` |
| Tipovi grešaka (404, 403, 409, 422) | `Application/Common/Izuzeci/Izuzeci.cs`, `Domain/Common/DomenskiIzuzetak.cs` |
| Serilog konfiguracija | `API/appsettings.json:60` i `Program.cs:21` |

### 2.8 Bot Detection middleware

Folder: `backend/RetroFit.API/Middleware/BotDetekcija/`

| Šta | Fajl:linija |
|---|---|
| Glavni tok obrade | `BotDetekcijaMiddleware.cs:38` |
| Funkcija odluke (propusti / 428 / 429 / blokada) | `BotDetekcijaMiddleware.cs:160` (`Odluci`) |
| Skor rizika 0-100 sa razlozima | `AnalizatorRizika.cs:42` |
| Klizni prozori, privremene blokade | `PracenjePonasanja.cs:38` |
| JS proof-of-work izazov | `JsIzazovServis.cs:25` |
| Otisak uređaja (HMAC), maskiranje IP-a | `KlijentskiSignali.cs` |
| Grupe ruta i limiti | `KlasifikacijaRuta.cs`, `BotDetekcijaOpcije.cs` |
| Upis u bazu (otisci, blokade, audit) | `BotDetekcijaSinhronizacija.cs:17` |
| Podešavanja (pragovi, limiti) | `API/appsettings.json:24-42` |
| Frontend: signali i rešavanje izazova | `frontend/src/api/bezbednost.ts`, `frontend/src/api/klijent.ts:79-80` |

### 2.9 Frontend

| Šta | Gde |
|---|---|
| Rute aplikacije | `frontend/src/App.tsx` |
| Axios klijent (token, 401 → refresh, 428 → izazov) | `frontend/src/api/klijent.ts` (refresh na 41, interceptor na 72) |
| Pozivi API-ja | `frontend/src/api/servisi.ts` |
| TypeScript tipovi (preslikani DTO-i) | `frontend/src/api/tipovi.ts` |
| Prijavljeni korisnik (sesija) | `frontend/src/kontekst/AuthKontekst.tsx` |
| Zaštićene rute po ulogama | `frontend/src/komponente/ZasticenaRuta.tsx` |
| Stranice | `frontend/src/stranice/` (17 fajlova = 18 stranica; `FormaOglasa` služi i za kreiranje i za izmenu) |

### 2.10 Testovi i DevOps

| Šta | Gde |
|---|---|
| Domenski testovi | `backend/RetroFit.Tests/Domen/` |
| Handleri sa Moq-om | `backend/RetroFit.Tests/Handleri/` |
| Validatori | `backend/RetroFit.Tests/Validatori/ValidatoriTests.cs` |
| Bot Detection | `backend/RetroFit.Tests/BotDetekcija/BotDetekcijaTests.cs` |
| Infrastruktura (FTS upit, JWT, heš, slike) | `backend/RetroFit.Tests/Infrastruktura/InfrastrukturaTests.cs` |
| Lažni Unit of Work za testove | `backend/RetroFit.Tests/Pomocne/LazniUnitOfWork.cs` |
| Docker Compose (baza + API + frontend) | `docker-compose.yml` |
| SQL Server image sa Full-Text-om | `docker/sqlserver-fts/Dockerfile` |
| CI (build, testovi, provera migracija, frontend, Docker) | `.github/workflows/ci.yml` |
| Centralne verzije paketa | `backend/Directory.Packages.props` |
| Fiksiran .NET SDK | `backend/global.json` |

---

## 3. Pitanja profesora sa odgovorima

Odgovori su kratki, onako kako ih treba reći usmeno. Uz odgovor je naveden fajl koji se može otvoriti ako profesor traži da vidi kod.

### 3.1 Opšta pitanja o projektu

**P: Ukratko, šta je RetroFit?**
O: Lokalni marketplace za polovne stvari. Korisnik može da prodaje, menja ili poklanja stvari. Ima pretragu (Full-Text, filteri, udaljenost), poruke, ponude i kontraponude, rezervaciju, kupovinu uz mock escrow, recenzije, omiljene oglase, zahteve "Tražim", prijave i moderaciju. Postoje tri uloge: korisnik, moderator i admin. Uz to je napravljen sopstveni middleware za detekciju botova.

**P: Zašto baš SQL Server?**
O: Zbog naprednih mogućnosti koje projekat koristi: Full-Text Search sa srpskim word breaker-om, prostorni tip `geography` sa SPATIAL indeksom, filtrirani indeksi, `rowversion` i rekurzivni CTE. EF Core ima zreo provajder za SQL Server, a Developer izdanje je besplatno.

**P: Code First ili Database First? Zašto?**
O: Code First sa migracijama. Šema je verzionisana zajedno sa kodom: svaka izmena je migracija u git-u, pa se baza na novom računaru pravi jednom komandom. CI proverava da migracije odgovaraju modelu. Ono što EF ne podržava (Full-Text i SPATIAL indeks) napisano je kao ručni SQL u posebnoj migraciji. Nikad se ne koristi `EnsureCreated`, jer ono zaobilazi migracije.

### 3.2 Dizajn baze i normalizacija

**P: Koliko tabela ima baza i koje su glavne?**
O: 29 tabela. Glavne su `Korisnici`, `Oglasi`, `Kategorije`, `Gradovi`, `SlikeOglasa`, `Razgovori`, `Poruke`, `Ponude`, `Rezervacije`, `Transakcije`, `Placanja`, `Recenzije` i `Prijave`. ER dijagram je u `docs/ER-dijagram.md`.

**P: U kojoj normalnoj formi je baza?**
O: Tabele su u 3NF: svaki neključni atribut zavisi od celog ključa i samo od ključa. Na primer, grad je posebna tabela, a ne tekst u oglasu. Kategorija je referenca, ne kopija naziva. Uloge idu preko vezne tabele `KorisnikUloge` (M:N). **Svesna odstupanja** su denormalizovane kolone `Korisnici.ProsecnaOcena`, `BrojRecenzija`, `BrojZavrsenihTransakcija` i `Oglasi.BrojOmiljenih` / `BrojPregleda`. One se mogu izračunati iz drugih tabela, ali bi se onda `AVG`/`COUNT` računao za svaku karticu na svakoj stranici rezultata. Ažuriraju se atomičnim UPDATE-om u istoj transakciji kao i recenzija, pa ne mogu da se "raziđu".

**P: Zašto su Rezervacije i Transakcije posebne tabele?**
O: To su različiti pojmovi. Rezervacija je privremeno zadržavanje oglasa za jednog kupca (72 h) i može da istekne ili se otkaže a da kupovina nikad ne nastane. Transakcija je sama kupoprodaja: način preuzimanja, escrow i potvrde obe strane. Veza je 1:1 (`Transakcije.RezervacijaId` je UNIQUE), a istorija rezervacija ostaje sačuvana.

**P: Zašto ste spojili Wishlist i Likes u jednu tabelu?**
O: Korisnik ima jednu listu omiljenih, pa bi `Wishlists` imala tačno jedan red po korisniku, bez ikakve informacije. `OmiljeniOglasi (KorisnikId, OglasId)` sa kompozitnim primarnim ključem sama sprečava duplikate.

**P: Zašto ste dodali tabelu Gradovi?**
O: Slobodan tekst ("Beograd", "beograd", "BG") ne može pouzdano da se filtrira. Uz to, tabela ima kolonu `Lokacija` tipa `geography`, što omogućava pretragu po udaljenosti uz SPATIAL indeks. U seed-u su 152 mesta sa koordinatama: sedišta svih 145 gradova i opština Srbije i prigradska mesta Beograda. Kolona `Naziv` ima srpsku kolaciju (`Serbian_Latin_100_CI_AS`), pa se lista sortira po našoj abecedi (C, Č, Ć...).

**P: Šta je EAV i zašto ga koristite?**
O: Entity-Attribute-Value: atributi se ne drže u kolonama, nego kao redovi. `DefinicijeAtributa` kaže koje atribute ima koja kategorija (npr. telefoni: memorija, boja; bicikli: veličina rama), a `AtributiOglasa` čuva vrednosti. Prednost: nova kategorija sa novim atributima ne zahteva izmenu šeme. Mana: teže je filtrirati po vrednosti i nema jake tipizacije u bazi. Zato je EAV korišćen samo za dodatne atribute, a sve što se često filtrira (cena, stanje, grad, tip) su prave kolone.

**P: Kako je modelovano stablo kategorija?**
O: Adjacency list: `Kategorije.RoditeljId` pokazuje na istu tabelu, uz `CHECK RoditeljId <> Id`. Stablo se čita rekurzivnim CTE-om (`KatalogRepozitorijumi.cs:54` i `:75`). Alternative su nested sets ili `hierarchyid`: brže čitaju, ali komplikuju upis. Za ~40 kategorija adjacency list + CTE je najjednostavnije i dovoljno brzo.

**P: Kako čuvate enume u bazi?**
O: Kao tekst (`'Aktivan'`, `'Prodat'`), uz `CHECK Kolona IN (...)`. U SSMS-u je odmah čitljivo, a nevalidna vrednost je nemoguća. Cena je malo više prostora nego `int`, što je zanemarljivo.

**P: Zašto ne koristite ASP.NET Identity?**
O: Identity pravi svojih 7 tabela sa engleskim imenima i kolonama koje nam ne trebaju. Napravili smo svoje `Korisnici`, `Uloge` i `KorisnikUloge`, koje su pod našom kontrolom i vidljive u šemi. Za heširanje lozinki ipak koristimo provereni `PasswordHasher` iz Identity-ja: PBKDF2 sa 100.000 iteracija i nasumičnom solju (`OsnovniServisi.cs:23`).

### 3.3 Integritet i ograničenja

**P: Kako baza garantuje da se oglas ne proda dvaput?**
O: Kroz dva sloja:
1. Filtrirani UNIQUE indeks `UX_Transakcije_OglasNeotkazana` na `Transakcije(OglasId) WHERE Status <> 'Otkazana'` (`TrgovinaKonfiguracije.cs:74`). Može postojati više otkazanih transakcija za oglas, ali samo jedna aktivna ili završena.
2. `rowversion` na `Oglasi`: ako dva zahteva istovremeno menjaju isti oglas, drugi dobija konflikt.

Ako baza odbije upis, `UnitOfWork.PrevediSqlGresku` prevodi SQL grešku 2601 u poruku "Oglas je već prodat..." i HTTP 409.

**P: Zašto pravila proveravate i u kodu i u bazi? Nije li to dupliranje?**
O: Imaju različite uloge. Domen daje jasnu poruku korisniku i pravilo se testira bez baze. Baza je konačna garancija: važi i kada dva zahteva stignu u istoj milisekundi (provera u kodu tada "prođe" za oba) i kada neko piše direktno u bazu. Ima ukupno tri sloja: FluentValidation (format ulaza), domen (poslovna pravila) i baza (CHECK, UNIQUE, FK).

**P: Koliko CHECK ograničenja imate? Navedite jedno zanimljivo.**
O: 70. Primer: cena zavisi od tipa oglasa, `(Tip='Prodaja' AND Cena > 0) OR (Tip IN ('Razmena','Poklon') AND Cena IS NULL)`. Drugi primer: završena transakcija mora imati obe potvrde i datum završetka. Recenzija ne može biti sam sebi: `CHECK OcenjivacId <> OcenjeniKorisnikId`.

**P: Kako sprečavate da korisnik ostavi dve recenzije za istu kupovinu?**
O: `UNIQUE (TransakcijaId, OcenjivacId)`. Po transakciji može postojati najviše dve recenzije (kupac ocenjuje prodavca i obrnuto), a isti korisnik ne može dvaput.

**P: Šta se dešava kada se obriše korisnik ili oglas?**
O: Ne briše se fizički, nego **meko** (`Obrisan` + `ObrisanoUtc`), jer se na njih oslanjaju transakcije i recenzije. Brisanje naloga je anonimizacija: lični podaci se brišu, red ostaje. Strani ključevi su podrazumevano RESTRICT (`RetroFitDbContext.cs:56-63`). CASCADE je uključen samo za "delove" entiteta: slike i atributi oglasa, poruke razgovora, tokeni korisnika itd. Istorija (transakcije, recenzije, plaćanja) se nikad ne briše kaskadno.

**P: Zašto niste koristili globalne query filtere za meko brisanje?**
O: Filter bi sakrio obrisan oglas i iz istorije transakcija: EF bi izbacio ceo red transakcije kod obavezne navigacije. Zato upiti eksplicitno dodaju `!Obrisan`, a indeksi za pretragu su filtrirani `WHERE Obrisan = 0`.

### 3.4 Indeksi

**P: Koje indekse imate i zašto?**
O: Osim primarnih ključeva, ima 21 UNIQUE i 19 filtriranih indeksa, plus SPATIAL i Full-Text. Najvažniji je pokrivajući indeks za pretragu: `(Status, KategorijaId, ObjavljenoUtc DESC) INCLUDE (Cena, GradId, Stanje, Tip) WHERE Obrisan = 0`. Najčešći upit (aktivni oglasi u kategoriji, najnoviji prvi) čita samo indeks, bez odlaska u tabelu.

**P: Šta je pokrivajući (covering) indeks?**
O: Indeks koji sadrži sve kolone koje upit traži, ključne plus `INCLUDE` kolone. SQL Server tada ne radi *key lookup* u glavnu tabelu. `INCLUDE` kolone su samo na listovima B-stabla, pa ne povećavaju ključ indeksa.

**P: Šta je filtrirani indeks i gde ga koristite?**
O: Indeks sa `WHERE` uslovom, koji sadrži samo redove koji ispunjavaju uslov. Koristim ga na dva načina:
1. **Kao pravilo:** "najviše jedna aktivna rezervacija po oglasu" je `UNIQUE (OglasId) WHERE Status='Aktivna'`. Običan UNIQUE bi zabranio i istoriju zatvorenih rezervacija.
2. **Za performanse:** pozadinski posao za istek oglasa traži samo aktivne oglase, pa je indeks `IsticeUtc WHERE Status='Aktivan' AND Obrisan=0` mali i brz.

**P: Zašto ne indeksirate svaku kolonu?**
O: Svaki indeks usporava INSERT/UPDATE/DELETE, jer se mora ažurirati, i zauzima prostor. Indeksi su napravljeni za konkretne upite koje aplikacija radi.

**P: Kako biste proverili da li se indeks koristi?**
O: U SSMS-u uključim *Include Actual Execution Plan* (Ctrl+M), izvršim upit i proverim da li plan radi *Index Seek* nad očekivanim indeksom, a ne *Table/Clustered Index Scan*. Može i `SET STATISTICS IO ON` za broj pročitanih stranica.

### 3.5 Full-Text Search

**P: Zašto Full-Text, a ne `LIKE '%bicikl%'`?**
O: `LIKE` sa `%` na početku ne može da koristi indeks. Radi *scan* cele tabele, i to sve sporije kako oglasa ima više. Full-Text pravi **invertovani indeks reči**: za svaku reč zna u kojim redovima se pojavljuje. Uz to podržava prefiksnu pretragu (`"bicik*"` pronalazi bicikl, bicikla, biciklistički), jezički word breaker i neosetljivost na kvačice.

**P: Kako je podešen?**
O: Katalog `RetroFitKatalog` sa `ACCENT_SENSITIVITY = OFF`, tako da "cacak" nalazi i "Čačak". Indeksi su nad `Oglasi(Naslov, Opis)` i `ZahteviTrazim(Naslov, Opis)`, jezik je srpski (latinica, LCID 2074). Sve je u migraciji `20261103124619_FullTextIProstorniIndeks.cs`.

**P: Kako sprečavate SQL injection kroz pretragu?**
O: Upit se gradi samo od slova i cifara (`FullText.cs:63`). Operatori kao `OR`, `NEAR`, `"` i `*` iz korisničkog unosa se odbacuju, a mi sami dodajemo `"reč*"` i `AND`. Vrednost se šalje kao SQL parametar, nikad lepljenjem stringa.

**P: Šta ako server nema instaliran Full-Text?**
O: `FullTextDetektor` (`FullText.cs:20`) na startu proverava `FULLTEXTSERVICEPROPERTY('IsFullTextInstalled')`. Ako komponenta ne postoji, pretraga automatski prelazi na `LIKE` i to se upiše u log. Aplikacija radi i bez nje, samo sporije.

**P: Koja je mana Full-Text-a?**
O: Indeks se puni **asinhrono**: novi oglas se u pretrazi pojavljuje posle kratkog kašnjenja (obično ispod sekunde). Pored toga, troši dodatni prostor, a pretraga po delu reči unutar reči (npr. "cikl" u "bicikl") ne radi, samo po prefiksu.

### 3.6 Prostorni podaci

**P: Kako radi pretraga "oglasi do 30 km"?**
O: `Gradovi.Lokacija` je tipa `geography` (SRID 4326, GPS koordinate). EF Core preko NetTopologySuite prevodi `IsWithinDistance(centar, metara)` u `Lokacija.STDistance(@centar) <= @metara` (`OglasRepository.cs:233`). Ubrzava ga SPATIAL indeks `SIX_Gradovi_Lokacija`.

**P: Razlika između `geography` i `geometry`?**
O: `geometry` računa na ravnoj površini (kao na papiru). `geography` računa na elipsoidu Zemlje, pa je `STDistance` u **metrima** i tačan na većim rastojanjima. Za GPS koordinate pravi izbor je `geography`.

**P: Zašto je lokacija na gradu, a ne na oglasu?**
O: Privatnost: tačna adresa prodavca se ne objavljuje. Grad je dovoljan za lokalni marketplace, a manje je redova u SPATIAL indeksu.

### 3.7 Transakcije i konkurentnost (često pitanje!)

**P: Gde koristite eksplicitne transakcije i zašto?**
O: Na mestima gde se više tabela mora promeniti "sve ili ništa". Primer je prihvatanje ponude (`Ponude.cs:90`): ponuda postaje prihvaćena, pravi se rezervacija, oglas postaje rezervisan, ostale ponude se odbijaju, šalje se sistemska poruka i obaveštenja. Ako bilo šta pukne, radi se rollback svega. Isto važi za kontraponudu, potvrdu transakcije i recenziju. Implementacija je `UnitOfWork.UTransakcijiAsync` (`UnitOfWork.cs:112`).

**P: Koji nivo izolacije koristite?**
O: `READ COMMITTED`, podrazumevani za SQL Server. Viši nivo (`SERIALIZABLE`) nije potreban, jer konflikte ne rešavamo zaključavanjem, nego **ograničenjima i `rowversion`-om**: filtrirani UNIQUE indeks garantuje "jednu aktivnu rezervaciju" bez obzira na izolaciju. Tako se izbegavaju dugi lock-ovi i deadlock-ovi.

**P: Objasnite optimističku konkurentnost.**
O: Tabele `Oglasi`, `Ponude`, `Rezervacije` i `Transakcije` imaju kolonu `rowversion`, koju SQL Server automatski menja pri svakoj izmeni reda. EF pri UPDATE-u dodaje `WHERE Id = @id AND RowVersion = @staraVerzija`. Ako je neko u međuvremenu izmenio red, UPDATE pogodi 0 redova, EF baci `DbUpdateConcurrencyException`, a mi vratimo 409 sa porukom "Podatak je u međuvremenu izmenjen" (`UnitOfWork.cs:99`).
**Pesimistička** bi zaključala red pri čitanju (`UPDLOCK`). Optimistička je bolja kada su konflikti retki, kao u web aplikaciji.

**P: Šta je "lost update" i kako ga izbegavate kod proseka ocena?**
O: Dve recenzije stignu istovremeno: obe pročitaju prosek 4.0 sa 10 recenzija, obe izračunaju novi prosek i upišu, pa se jedna izmena izgubi. Rešenje je atomični UPDATE (`KorisnikRepozitorijumi.cs:86-93`):
`UPDATE Korisnici SET ProsecnaOcena = ROUND((ProsecnaOcena * BrojRecenzija + @ocena) / (BrojRecenzija + 1), 2), BrojRecenzija = BrojRecenzija + 1 WHERE Id = @id`.
Računanje radi baza, nad trenutnom vrednošću, a red je zaključan tokom same naredbe, pa se izmene ne gube.

**P: Šta je execution strategy?**
O: Mehanizam EF Core-a koji ponavlja operaciju kod prolaznih grešaka, npr. kratkog prekida veze. Kod eksplicitne transakcije **cela** transakcija mora biti unutar strategije (`UnitOfWork.cs:121`), da se pri ponovnom pokušaju ponovi ceo blok, a ne samo poslednja naredba.

**P: Zašto zamena redosleda dve slike ne krši `UNIQUE (OglasId, Redosled)`?**
O: Redosled se menja jednom UPDATE naredbom sa `CASE` (`OglasRepository.cs:154`). SQL Server proverava UNIQUE ograničenje na kraju naredbe, a ne posle svakog reda, pa trenutni "duplikat" usred naredbe nije problem. Dve odvojene UPDATE naredbe bi pukle.

### 3.8 EF Core i upiti

**P: Kako izbegavate N+1 problem?**
O: Upiti za čitanje koriste **projekciju** u DTO (`ProjectTo` / `Select`). EF tada pravi jedan SQL sa JOIN-ovima i bira samo potrebne kolone. Kod komandi se koristi `Include` tamo gde treba.

**P: Šta je `AsNoTracking` i zašto ga koristite?**
O: EF tada ne prati izmene učitanih entiteta. To je brže i troši manje memorije. Svi upiti (Query) ga koriste, a komande (Command) ne, jer menjaju entitete.

**P: Zašto su svi datumi u UTC-u?**
O: Server i korisnici mogu biti u različitim zonama, a letnje računanje vremena pravi rupe i duplikate u lokalnom vremenu. U bazi je sve `datetime2(3)` u UTC-u. Konverter u `RetroFitDbContext.cs` označava pročitane datume kao UTC, pa JSON nosi sufiks `Z`, a frontend prikazuje lokalno vreme.

**P: Kako radi audit log?**
O: `AuditInterceptor` (`AuditInterceptor.cs:23`) se uključuje pri svakom `SaveChanges`. Za "važne" entitete beleži ko je, kada i šta promenio, sa starim i novim vrednostima kao JSON-om (`CHECK ISJSON(...) = 1`). Lozinke, emailovi, telefoni i tokeni su maskirani.

### 3.9 Arhitektura i šabloni

**P: Šta je Clean Architecture i zašto je korišćena?**
O: Zavisnosti idu samo ka unutra: API → Application → Domain, a Infrastructure implementira interfejse iz Application. Domain ne zna za bazu, web ni biblioteke, pa se poslovna pravila testiraju bez baze. Baza ili framework mogu da se zamene bez diranja pravila.

**P: Šta je CQRS i kako ga koristite?**
O: Command Query Responsibility Segregation: operacije koje **menjaju** stanje (`*Command`) odvojene su od onih koje **čitaju** (`*Query`). Komande idu kroz domenske entitete i Unit of Work. Upiti čitaju direktno u DTO, sa `AsNoTracking` i projekcijom. Ima 96 komandi i upita. Ovo nije CQRS sa dve baze, nego na nivou koda, nad istom bazom.

**P: Šta radi MediatR?**
O: Kontroler ne poziva handler direktno, nego šalje poruku (`mediator.Send(command)`). MediatR pronalazi handler i pušta zahtev kroz **pipeline behaviors**: logovanje (`Ponasanja.cs:39`) i validaciju (`Ponasanja.cs:12`). Validacija i logovanje se zato ne pišu u svakom handleru.

**P: Repository i Unit of Work, zar EF već nije to?**
O: Jeste: DbSet je repozitorijum, a DbContext je Unit of Work. Svoji interfejsi su ipak uvedeni, jer Application sloj ne sme da zavisi od EF Core-a (Clean Architecture). Testovi koriste lažni Unit of Work (`backend/RetroFit.Tests/Pomocne/LazniUnitOfWork.cs`), a specifični upiti (FTS, CTE, atomični UPDATE) su skriveni iza metoda sa jasnim imenima. Repozitorijumi nikad ne zovu `SaveChanges`. To radi samo Unit of Work, pa jedna komanda čuva sve izmene odjednom.

**P: Šta znači "bogat domenski model"?**
O: Entiteti nisu samo kontejneri podataka sa getter-ima i setter-ima, nego imaju ponašanje. `oglas.Rezervisi()` proverava da li je oglas aktivan, a nedozvoljen prelaz baca `DomenskiIzuzetak` (HTTP 422). Setteri su `private`, pa se stanje ne može promeniti mimo pravila.

**P: Kako izgledaju greške koje API vraća?**
O: Uvek u istom obliku, kao **ProblemDetails** (RFC 7807): `status`, `title`, `detail`, plus naša polja `kod` i `traceId`. Klijent nikad ne dobija stack trace. Sa `traceId` se odgovarajući zapis pronalazi u logu. Pravi ih `GlobalniExceptionHandler.cs:16`.

**P: Zašto API verzionisanje?**
O: Ako se ugovor API-ja promeni, stari klijenti (npr. mobilna aplikacija koju korisnici nisu ažurirali) nastavljaju da rade na `/api/v1/`, a novi koriste `/api/v2/`.

### 3.10 Bezbednost

**P: Kako radi autentifikacija?**
O: JWT access token traje 15 minuta i drži se samo u memoriji frontenda. Refresh token traje 14 dana i nalazi se u **httpOnly, SameSite=Strict** kolačiću: JavaScript ga ne vidi, pa ga XSS ne može ukrasti, a drugi sajt ga ne može poslati (CSRF). U bazi se čuva samo SHA-256 heš refresh tokena.

**P: Šta je rotacija refresh tokena i detekcija ponovne upotrebe?**
O: Pri svakom osvežavanju stari token se opoziva i izdaje se novi, u istoj "porodici". Ako neko pošalje već iskorišćen token, to znači da ga je neko ukrao: ili napadač ili žrtva šalje staru kopiju. Tada se opoziva **cela porodica** i beleži bezbednosni događaj, pa obe strane moraju ponovo da se prijave (`Komande.cs:182`).

**P: Šta ako dva taba istovremeno osvežavaju token? Neće li to izgledati kao krađa?**
O: Tome služi tolerancija od 30 sekundi (`Komande.cs:220`). Ako je token rotiran pre manje od 30 s i njegov naslednik je još važeći, zahtev se ne tretira kao krađa (isti pristup kao "reuse interval" kod Auth0). Frontend dodatno spaja istovremena osvežavanja u jedan zahtev (`klijent.ts:41`).

**P: Kako se čuvaju lozinke?**
O: PBKDF2-HMAC-SHA512 sa 100.000 iteracija i nasumičnom solju od 128 bita (format ASP.NET Identity V3). Nikad kao plain-text, i nikad kao običan SHA-256, jer je prebrz za napad grubom silom.

**P: Kako sprečavate otkrivanje koji emailovi postoje u sistemu?**
O: Pogrešan email i pogrešna lozinka daju istu poruku. Za nepostojeći nalog se ipak proverava lažni heš, pa vreme odgovora ne otkriva da li nalog postoji.

**P: Ko vidi telefon prodavca?**
O: Samo kupac sa aktivnom ili realizovanom rezervacijom, i to samo ako je prodavac verifikovao broj i uključio opciju prikaza (`Upiti.cs:55`). Javni DTO-i ne sadrže email, telefon ni adresu.

**P: Kako se proverava otpremljena slika?**
O: Po potpisu fajla (magic bytes), ne po ekstenziji, jer se `virus.exe` lako preimenuje u `slika.jpg`. Dozvoljeni su samo JPEG, PNG i WebP do 5 MB. Ime fajla generiše server (GUID).

### 3.11 Bot Detection middleware

**P: Kako radi vaš Bot Detection?**
O: Za svaki zahtev middleware (`BotDetekcijaMiddleware.cs:38`):
1. odredi grupu rute (prijava, registracija, poruke, pretraga...);
2. napravi otisak uređaja kao HMAC od grubih signala (pregledač, jezik, ekran, vremenska zona, platforma);
3. proveri blokade;
4. broji zahteve u kliznim prozorima po grupi;
5. izračuna **skor rizika 0-100** sa razlozima (`AnalizatorRizika.cs:42`).

Na osnovu skora odluči (`Odluci`, linija 160): propusti, traži JS izazov (428), ograniči (429) ili privremeno blokira. Posle tri privremene blokade za 24 h uređaj se trajno blokira u bazi.

**P: Koji signali povećavaju rizik?**
O: Na primer: nema User-Agent (+40), User-Agent alata kao curl ili python-requests (+35), `navigator.webdriver = true` (+40), spam (ista poruka u ≥ 3 razgovora, +40), otvaranje oglasa redom po ID-ju, tj. enumeracija (+35), "robotski ritam" sa previše ravnomernim razmacima između zahteva (+25). Cela tabela je u `docs/bot-detekcija.md`.

**P: Šta je JS izazov i zašto ne CAPTCHA?**
O: Proof-of-work: pregledač mora da pronađe broj X takav da `SHA-256("nonce:X")` počinje sa 16 nultih bitova, oko 65.000 heširanja, otprilike 1 sekunda (`JsIzazovServis.cs:25`). Čovek to ne primeti, a masovna automatizacija postaje skupa. Izazov je potpisan HMAC-om, pa server ne mora da pamti izdate izazove. Replay je sprečen jer se nonce prihvata samo jednom.
**Iskreno ograničenje:** ovo ne dokazuje da je klijent čovek, samo poskupljuje napad. Zato je to jedan sloj od više.

**P: Da li je IP adresa dobar identifikator?**
O: Nije. IP dele mnogi korisnici (NAT, kampusi, mobilni operateri) i lako se menja (VPN). Zato IP nije deo otiska. Koristi se samo kao dodatni limit za prijavu i registraciju, a u logovima je maskiran (IPv4 /24).

**P: A TLS otisak (JA3/JA4)?**
O: Računa se iz TLS handshake-a, a kada TLS završava reverse proxy, aplikacija taj handshake ne vidi. Zato se otisak prihvata samo ako ga prosledi **pouzdan** proxy u zaglavlju `X-TLS-Fingerprint`. Od bilo koga drugog se ignoriše.

**P: Gde se čuva stanje (brojači)?**
O: U memoriji API-ja, jer je brzo. Periodično (na 30 s) se statistika upisuje u bazu (`OtisciUredjaja`, `BlokiraniUredjaji`, `AuditZapisi`). Ograničenje: za više instanci API-ja isti interfejs bi se implementirao preko Redis-a.

**P: Zašto u razvoju ne blokira?**
O: U `appsettings.Development.json` je režim `SamoPracenje`: računa rizik i loguje šta bi uradio, ali ne blokira, da ne smeta tokom razvoja. Produkcija i Docker koriste režim `Zastita`.

### 3.12 Testovi i DevOps

**P: Šta testirate?**
O: 162 testa:
- mašine stanja entiteta (oglas, ponuda, rezervacija, transakcija);
- validatore;
- handlere sa Moq-om (prijava, rotacija tokena, krađa tokena, prihvatanje ponude, privatnost telefona, ovlašćenja moderatora);
- ceo Bot Detection (skor, odluke, klizni prozori, spam, JS izazov);
- infrastrukturu (FTS upit, JWT, heš lozinki, prepoznavanje slika).

**P: Zašto nema integracionih testova sa pravom bazom?**
O: Ograničenja u bazi (UNIQUE, CHECK, rowversion) su provereni ručno i kroz demo. Sledeći korak bi bili integracioni testovi sa Testcontainers (SQL Server u Docker-u) ili `WebApplicationFactory`. To je dobro reći kao "šta bih unapredio".

**P: Šta radi CI?**
O: GitHub Actions (`.github/workflows/ci.yml`) na svaki push: build, testovi, provera da migracije odgovaraju modelu, frontend lint i build, build Docker image-a.

**P: Šta radi Docker Compose?**
O: Podiže tri kontejnera: SQL Server sa Full-Text-om (poseban image, `docker/sqlserver-fts/Dockerfile`, jer zvanični image nema FTS), API i frontend (nginx). API sam primeni migracije. Podaci ostaju u volume-ima. Tajne su u `.env` fajlu, ne u kodu.

### 3.13 "Zamka" pitanja

**P: Šta biste unapredili da imate više vremena?**
O:
- integracione testove sa pravom bazom (Testcontainers);
- WebSocket / SignalR za poruke umesto periodičnog osvežavanja (polling);
- Redis za stanje Bot Detection-a, da bi API mogao da radi u više instanci;
- pravog platnog provajdera umesto mock escrow-a;
- slike u objektnom skladištu (Azure Blob / S3) umesto na disku.

**P: Šta je najteže bilo?**
O: Konkurentnost: da se oglas ne proda dvaput, da se prosek ocena ne pokvari pri istovremenim recenzijama i da dva taba ne "izloguju" korisnika pri istovremenom osvežavanju tokena. Rešeno je kombinacijom filtriranih UNIQUE indeksa, `rowversion`-a, atomičnih UPDATE-a i tolerancije od 30 s.

**P: Zašto AutoMapper 13, a ne najnoviji?**
O: Od verzije 15 AutoMapper (kao i MediatR 13+ i FluentAssertions 8+) ima komercijalnu licencu. Izabrane su poslednje besplatne verzije. AutoMapper ima poznatu ranjivost (DoS kod duboko ugnježdenih objekata) koja ovde nije iskoristiva: mapiramo samo entitete iz baze, nikad korisnički ulaz, JSON je ograničen na dubinu 32 i sve mape imaju `MaxDepth(3)`. Detalji su u `docs/bezbednost.md`.

**P: Da li je aplikacija spremna za produkciju?**
O: Arhitektura i bezbednost jesu: tajne kroz okruženje, HTTPS kolačići, bezbednosna zaglavlja, logovi, Docker. Mock servisi (SMS, plaćanje) i stanje u memoriji jedne instance bi se zamenili pravim servisima i Redis-om.

**P: Šta se desi ako baza padne?**
O: Execution strategy ponovi prolazne greške. Ako baza i dalje ne odgovara, korisnik dobija ProblemDetails sa `traceId`, a ne stack trace. `/health/ready` tada vraća grešku, pa bi orkestrator (Docker, Kubernetes) to video.

---

## 4. SQL upiti za demonstraciju u SSMS-u

```sql
USE RetroFit;

-- 1) Pregled: koliko čega ima
SELECT 'Korisnici' T, COUNT(*) N FROM Korisnici UNION ALL
SELECT 'Oglasi', COUNT(*) FROM Oglasi UNION ALL
SELECT 'Kategorije', COUNT(*) FROM Kategorije UNION ALL
SELECT 'Gradovi', COUNT(*) FROM Gradovi;

-- 2) Sva CHECK ograničenja tabele Oglasi
SELECT name, definition FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.Oglasi');

-- 3) Svi filtrirani indeksi
SELECT OBJECT_NAME(object_id) AS Tabela, name, filter_definition
FROM sys.indexes WHERE has_filter = 1 ORDER BY Tabela;

-- 4) Full-Text pretraga
SELECT Id, Naslov FROM Oglasi WHERE CONTAINS((Naslov, Opis), N'"bicik*"');

-- 5) Gradovi do 70 km od Beograda (geography)
DECLARE @bg geography = (SELECT Lokacija FROM Gradovi WHERE Id = 1);
SELECT Naziv, ROUND(Lokacija.STDistance(@bg) / 1000, 1) AS Km
FROM Gradovi WHERE Lokacija.STDistance(@bg) <= 70000 ORDER BY Km;

-- 6) Rekurzivni CTE: kategorija i sve njene podkategorije
WITH Stablo AS (
    SELECT Id, Naziv, RoditeljId, 0 AS Nivo FROM Kategorije WHERE RoditeljId IS NULL
    UNION ALL
    SELECT k.Id, k.Naziv, k.RoditeljId, s.Nivo + 1 FROM Kategorije k JOIN Stablo s ON k.RoditeljId = s.Id
)
SELECT REPLICATE('    ', Nivo) + Naziv AS Kategorija FROM Stablo ORDER BY RoditeljId, Naziv;

-- 7) Baza odbija nevalidan podatak (CHECK): poklon sa cenom
-- Očekivano: greška 547 (CHECK constraint)
UPDATE Oglasi SET Tip = 'Poklon' WHERE Id = (SELECT TOP 1 Id FROM Oglasi WHERE Tip = 'Prodaja');

-- 8) Denormalizovan prosek ocena = stvaran prosek iz recenzija
SELECT k.Ime, k.ProsecnaOcena, k.BrojRecenzija,
       (SELECT AVG(CAST(r.Ocena AS decimal(3,2))) FROM Recenzije r WHERE r.OcenjeniKorisnikId = k.Id) AS StvarniProsek
FROM Korisnici k WHERE k.BrojRecenzija > 0;

-- 9) Poslednji audit zapisi i bezbednosni događaji
SELECT TOP 20 * FROM AuditZapisi ORDER BY Id DESC;

-- 10) Refresh tokeni: rotacija u porodici (vidi se ZamenjenHashom i RazlogOpoziva)
SELECT TOP 10 KorisnikId, PorodicaId, KreiranoUtc, OpozvanoUtc, RazlogOpoziva FROM RefreshTokeni ORDER BY Id DESC;
```

> Upit 7 **namerno** pada: tako se pokazuje da baza sama čuva pravila. Ako ikad i prođe, podatak treba vratiti ručno ili resetovati bazu (deo 1).

---

## 5. Scenario demonstracije (~10 minuta)

1. **Uvod (1 min):** šta je aplikacija, uloge, stack. Pokazati `README.md` i strukturu foldera.
2. **Arhitektura (1 min):** folder `backend` (četiri projekta + testovi) pored foldera `frontend`, i dijagram iz `docs/arhitektura.md`.
3. **Sajt kao Marko (3 min):**
   - pretraga "bicikl" (Full-Text), filter po kategoriji i udaljenosti;
   - detalji oglasa, zatim poruka prodavcu i ponuda;
   - odjava, prijava kao prodavac, pa prihvatanje ponude: nastaje rezervacija i ostale ponude se odbijaju;
   - kupovina, obe potvrde, recenzija.
4. **SSMS (3 min):** upiti 2, 3, 4, 5 i 7 iz dela 4. Pokazati ER dijagram (`docs/ER-dijagram.md`, prikaz u VS Code-u ili na GitHub-u).
5. **Kod (1 min):** `PrihvatiPonuduHandler` (`Ponude.cs:90`) kao primer transakcije i domenske logike, plus `UnitOfWork.PrevediSqlGresku`.
6. **Bot Detection (1 min):** pokrenuti backend u režimu `Zastita` i u Git Bash-u izvršiti:
   ```bash
   curl -s -A "python-requests/2.31" http://localhost:5029/api/v1/bezbednost/procena
   ```
   Prikazuje skor i razloge. Zatim admin tabla, kartica "Uređaji".
7. **Testovi (30 s):** u folderu `backend` pokrenuti `dotnet test`, 162 zelena testa.

### Ako nešto ne radi na odbrani

| Problem | Rešenje |
|---|---|
| Backend: "Cannot open database" / timeout | Pokrenuti servis `SQL Server (MSSQLSERVER)` u `services.msc` |
| Frontend: "Network Error" | Backend nije pokrenut ili radi na drugom portu (mora 5029) |
| "Port already in use" | Stara instanca radi: zatvoriti terminal ili `taskkill /IM RetroFit.API.exe /F` |
| Pregledač javlja 428 / 429 | Backend je pokrenut u režimu `Zastita` i limit je dostignut: sačekati minut ili ga pokrenuti bez `--BotDetekcija:Rezim=Zastita` |
| Pretraga ne nalazi novi oglas odmah | Full-Text indeks se puni asinhrono, sačekati sekundu-dve |
