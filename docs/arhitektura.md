# Arhitektura

RetroFit koristi **Clean Architecture**: zavisnosti idu samo ka unutra, a poslovna pravila ne zavise od baze, web-a ni biblioteka.

```
            ┌──────────────────────────────────────────────┐
            │  RetroFit.API  (kontroleri, middleware, DI)  │
            └───────────────┬──────────────────────────────┘
                            │ šalje Command/Query (MediatR)
            ┌───────────────▼──────────────────────────────┐
            │  RetroFit.Application                         │
            │  CQRS handleri, DTO, validatori, interfejsi   │◄────┐
            └───────────────┬──────────────────────────────┘     │ implementira
                            │ koristi                             │ interfejse
            ┌───────────────▼──────────────┐     ┌────────────────┴───────────────┐
            │  RetroFit.Domain              │◄────┤  RetroFit.Infrastructure        │
            │  entiteti + poslovna pravila  │     │  EF Core, repozitorijumi, UoW,  │
            │  (bez ikakvih zavisnosti)     │     │  JWT, mock servisi, poslovi     │
            └──────────────────────────────┘     └────────────────────────────────┘
```

| Projekat | Sadrži | Zavisi od |
|---|---|---|
| `RetroFit.Domain` | Entiteti sa ponašanjem (mašine stanja), enumi, `DomenskiIzuzetak`, poslovne konstante, pravila reputacije | ničega (ni jedan NuGet paket) |
| `RetroFit.Application` | Komande i upiti (CQRS) sa handlerima, FluentValidation validatori, DTO, AutoMapper profil, interfejsi repozitorijuma i servisa, MediatR ponašanja | Domain |
| `RetroFit.Infrastructure` | `RetroFitDbContext`, konfiguracije entiteta, migracije, repozitorijumi, Unit of Work, audit interceptor, JWT, heširanje, mock SMS/plaćanje, skladište slika, pozadinski poslovi, seed | Application |
| `RetroFit.API` | Tanki kontroleri, globalni exception handler, Bot Detection middleware, Serilog, Swagger, verzionisanje, JWT autentifikacija i politike | Application, Infrastructure (samo za DI) |
| `RetroFit.Tests` | xUnit + Moq + FluentAssertions (162 testa) | svi |

## Tok jednog zahteva (primer: prihvatanje ponude)

1. `POST /api/v1/ponude/42/prihvati` → **BotDetekcijaMiddleware** (rizik, rate limit) → JWT autentifikacija → autorizacija.
2. `TrgovinaKontroler.Prihvati` samo šalje `PrihvatiPonuduCommand` kroz MediatR.
3. MediatR pipeline: **LogovanjePonasanje** (trajanje, ishod) → **ValidacijaPonasanje** (FluentValidation) → handler.
4. `PrihvatiPonuduHandler` u `IUnitOfWork.UTransakcijiAsync`:
   učitava ponudu preko repozitorijuma → `ponuda.Prihvati(...)` (domensko pravilo) → `Rezervacija.Kreiraj(...)` (oglas → Rezervisan) → odbija ostale ponude → poruka + obaveštenja → `SacuvajAsync`.
5. `AuditInterceptor` popunjava audit polja i upisuje audit zapise.
6. Ako baza odbije upis (UNIQUE/RowVersion), `UnitOfWork` vraća `KonfliktIzuzetak` → **GlobalniExceptionHandler** → `409 ProblemDetails`.

## Šabloni (zahtevi profesora)

| Šablon | Implementacija |
|---|---|
| **Repository** | `IRepository<T>` + 20 specifičnih (`IOglasRepository`, ...) u Application; implementacije u `Infrastructure/Persistencija/Repozitorijumi`. Repozitorijumi nikad ne pozivaju `SaveChanges`. |
| **Unit of Work** | `IUnitOfWork` (svi repozitorijumi + `SacuvajAsync` + `UTransakcijiAsync`); jedan DbContext po zahtevu. |
| **CQRS + MediatR** | `*Command` menjaju stanje (preko entiteta i UoW), `*Query` samo čitaju (AsNoTracking + projekcija u SQL-u). Kontroleri ne znaju za DbContext. |
| **DTO + AutoMapper** | `MapiranjeProfil` (Map i ProjectTo); DTO-i namerno ne sadrže privatna polja. |
| **FluentValidation** | validator po komandi, automatski kroz `ValidacijaPonasanje`; poruke na srpskom. |
| **Dependency Injection** | `AddApplication()`, `AddInfrastructure()`, `AddApi()`. |
| **Global Exception Handler + ProblemDetails** | `GlobalniExceptionHandler` (`IExceptionHandler`) + prilagođen `InvalidModelStateResponseFactory` i `CustomizeProblemDetails` - SVE greške imaju isti oblik sa poljima `kod` i `traceId`. |
| **API verzionisanje** | `Asp.Versioning`, URL segment `/api/v1/`. |
| **Serilog** | strukturisani logovi (konzola + JSON fajl), korelacioni ID u svakom logu, request logging sa korisnikom i IP adresom. |

## Domenski model (bogat, ne anemičan)

Svaki prelaz stanja je metoda entiteta koja proverava pravilo, npr. `Oglas.Rezervisi()`, `Ponuda.Kontraponuda(...)`, `Transakcija.Potvrdi(...)`. Nedozvoljen prelaz baca `DomenskiIzuzetak` (HTTP 422). Pravila su zato na jednom mestu i lako se testiraju bez baze.

```
Oglas:       Nacrt → Aktivan ⇄ Rezervisan → Prodat
                      Aktivan → Istekao → (obnova) Aktivan
                      bilo koji osim Prodat → Uklonjen (moderator)
Ponuda:      NaCekanju → Prihvacena | Odbijena | Istekla | Kontraponuda (+ nova ponuda)
Rezervacija: Aktivna → Realizovana (nastala transakcija) | Otkazana | Istekla (72h)
Transakcija: UToku → Zavrsena (obe potvrde) | Otkazana
Escrow:      NijeKoriscen | CekaUplatu → Deponovano → Isplaceno | Vraceno
```

## Pozadinski poslovi

`PeriodicniPoslovi` (BackgroundService, na 5 min) šalje MediatR komande: istek oglasa (60 dana), istek ponuda (48h), istek rezervacija (72h), obaveštenja za sačuvane pretrage, čišćenje starih refresh tokena. `BotDetekcijaSinhronizacija` (na 30 s) upisuje statistiku otisaka i bezbednosne događaje u bazu.

## Frontend

React 19 + TypeScript + Vite + Tailwind CSS 4 + React Router 7 + Axios.
- `src/api` - tipovi (preslikani DTO-i), Axios klijent sa interceptorima (access token u memoriji, automatsko osvežavanje na 401, automatsko rešavanje JS izazova na 428, klijentski signali), servisne funkcije.
- `src/kontekst` - prijavljeni korisnik (sesija se vraća iz httpOnly cookie-ja), toast poruke.
- `src/komponente` - Layout (navigacija sa brojačima i menijem po ulogama), kartica oglasa, zaštićena ruta, UI elementi.
- `src/stranice` - 18 stranica (početna, prijava, registracija, oglasi, detalji, kreiranje/izmena, profil, javni profil, poruke, ponude, kupovine, omiljeni, "Tražim", detalji zahteva, obaveštenja, moderator i admin tabla).
- Zaštićene rute i UI po ulogama su samo udobnost - **prava zaštita je na serveru**.
