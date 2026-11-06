# Dizajn baze podataka

SQL Server 2022, EF Core 8 **Code First** sa migracijama (`backend/RetroFit.Infrastructure/Persistencija/Migracije`).
Šema se pravi isključivo migracijama (`Database.MigrateAsync`), nikad preko `EnsureCreated`.

## 1. Odluke o modelu (u odnosu na početnu listu entiteta)

| Odluka | Obrazloženje |
|---|---|
| `ListingLikes` + `Wishlists` + `WishlistItems` → jedna tabela **`OmiljeniOglasi`** (KorisnikId, OglasId) | Korisnik ima jednu listu omiljenih, pa bi `Wishlists` imala tačno jedan red po korisniku - suvišna tabela bez informacije. Kompozitni PK sam sprečava duplikate. |
| **`Rezervacije`** kao posebna tabela, odvojena od `Transakcije` | Rezervacija je *privremeno zadržavanje* oglasa za jednog kupca (72h); transakcija je *sama kupoprodaja* (način preuzimanja, escrow, potvrde). Rezervacija može da istekne ili se otkaže a da transakcija nikad ne nastane; istorija rezervacija ostaje. Veza 1:1 (`Transakcije.RezervacijaId` UNIQUE). |
| Dodata tabela **`Gradovi`** sa kolonom tipa `geography` | Pouzdan filter po gradu (umesto slobodnog teksta) i pretraga po udaljenosti uz SPATIAL indeks. |
| **`BlokiraniKorisnici`** = istorija banova | Kolona `IsBlocked` ne bi čuvala ko je, zašto i do kada blokirao, niti da li je blokada ukinuta. |
| Sumnjive aktivnosti → **`AuditZapisi`** (kategorija `Bezbednost`) | Ista struktura kao ostali audit zapisi - nema potrebe za novom tabelom. |
| Svoje tabele `Korisnici/Uloge/KorisnikUloge` umesto ASP.NET Identity | Tabele su pod našom kontrolom i vidljive u šemi; lozinke heširamo proverenim `PasswordHasher<T>` (PBKDF2, 100.000 iteracija, so). |

## 2. Kako baza garantuje poslovna pravila

Pravila se proveravaju **dva puta**: u domenskom modelu (jasna poruka korisniku) i u bazi (konačna garancija - važi i kada dva zahteva stignu istovremeno, i kada neko piše direktno u bazu).

| # | Pravilo | Ograničenje u bazi |
|---|---|---|
| 1 | Oglas ima više slika, najviše 10 | `SlikeOglasa.OglasId` FK (CASCADE); `UNIQUE (OglasId, Redosled)`; `CHECK Redosled BETWEEN 0 AND 9` |
| 2 | Kategorija ima podkategorije | `Kategorije.RoditeljId` → `Kategorije.Id` (RESTRICT); `CHECK RoditeljId <> Id` |
| 3 | Korisnik ima više oglasa | `Oglasi.ProdavacId` FK (RESTRICT) |
| 4 | Oglas ima više ponuda | `Ponude.OglasId` FK; filtrirani `UNIQUE (RazgovorId) WHERE Status='NaCekanju'` - jedna ponuda na čekanju po razgovoru |
| 5 | Oglas ima više poruka | `Razgovori` (UNIQUE `(OglasId, KupacId)`) → `Poruke` |
| 6-7 | Transakcija ima najviše 2 recenzije; isti korisnik ne ocenjuje dvaput | `UNIQUE (TransakcijaId, OcenjivacId)` + `CHECK OcenjivacId <> OcenjeniKorisnikId` + `CHECK Ocena BETWEEN 1 AND 5` |
| 8 | Oglas se ne prodaje dvaput | filtrirani `UNIQUE (OglasId) WHERE Status <> 'Otkazana'` na `Transakcije` + `RowVersion` na `Oglasi` |
| 9 | Rezervacija pripada konkretnom kupcu | `Rezervacije.KupacId NOT NULL`, `CHECK KupacId <> ProdavacId`, filtrirani `UNIQUE (OglasId) WHERE Status='Aktivna'` |
| 10 | Privatnost | Telefon/heš lozinke/tokeni ne postoje u javnim DTO-ima; telefon prodavca se vraća samo kupcu sa aktivnom/realizovanom rezervacijom |

Ostala važna ograničenja (ukupno **70 CHECK** ograničenja):
- cena po tipu: `(Tip='Prodaja' AND Cena > 0) OR (Tip IN ('Razmena','Poklon') AND Cena IS NULL)`;
- `CHECK DostavaMoguca = 1 OR LicnoPreuzimanjeMoguce = 1`;
- objavljen oglas mora imati datume objave i isteka: `Status='Nacrt' OR (ObjavljenoUtc IS NOT NULL AND IsticeUtc IS NOT NULL)`;
- završena transakcija mora imati obe potvrde i datum: `Status <> 'Zavrsena' OR (KupacPotvrdioUtc IS NOT NULL AND ProdavacPotvrdioUtc IS NOT NULL AND ZavrsenoUtc IS NOT NULL)`;
- dostava mora imati adresu; escrow zahteva cenu; escrow knjiženja moraju imati transakciju;
- podignut oglas mora imati plaćanje, admin-izdvojen ne sme;
- audit JSON kolone: `CHECK ISJSON(...) = 1`;
- svi enumi se čuvaju kao tekst uz `CHECK Kolona IN ('A','B',...)` - čitljivo u SSMS-u, a nevalidna vrednost je nemoguća.

## 3. Ponašanje pri brisanju

Podrazumevano je **RESTRICT** (DbContext pretvara sve konvencijske CASCADE veze u RESTRICT). CASCADE je eksplicitno uključen samo za "delove" entiteta:
`SlikeOglasa`, `AtributiOglasa`, `OmiljeniOglasi`, `IstaknutiOglasi` (od oglasa), `Poruke` (od razgovora), `KorisnikUloge`, `RefreshTokeni`, `VerifikacijeKorisnika`, `SacuvanePretrage`, `Obavestenja` (od korisnika), `OdgovoriNaZahteve` (od zahteva).
Istorija (transakcije, recenzije, ponude, plaćanja) se **nikad** ne briše kaskadno. `AuditZapisi.KorisnikId` i `OtisciUredjaja.PoslednjiKorisnikId` su `SET NULL`.

**Meko brisanje** (`Obrisan` + `ObrisanoUtc`) za `Korisnici` i `Oglasi`, jer se na njih oslanjaju transakcije i recenzije. Brisanje naloga = anonimizacija ličnih podataka.
Namerno NISU korišćeni globalni query filteri: filter bi "sakrio" obrisan oglas i iz istorije transakcija (EF bi izbacio redove kod obaveznih navigacija). Umesto toga upiti eksplicitno dodaju `!Obrisan`, a indeksi za pretragu su filtrirani (`WHERE Obrisan = 0`).

## 4. Indeksi

- **Pretraga oglasa:** `(Status, KategorijaId, ObjavljenoUtc DESC) INCLUDE (Cena, GradId, Stanje, Tip) WHERE Obrisan = 0` - pokrivajući indeks za najčešći upit; plus indeksi za cenu, grad, popularnost.
- **Pozadinski poslovi:** `IsticeUtc WHERE Status='Aktivan' AND Obrisan=0` (istek oglasa), `IsticeUtc WHERE Status='NaCekanju'` (ponude), `IsticeUtc WHERE Status='Aktivna'` (rezervacije) - mali filtrirani indeksi koji sadrže samo redove koje posao obrađuje.
- **Nepročitano:** `Poruke (RazgovorId, PosiljalacId) WHERE ProcitanoUtc IS NULL`, `Obavestenja (KorisnikId) WHERE ProcitanoUtc IS NULL`.
- **SPATIAL** `SIX_Gradovi_Lokacija` (GEOGRAPHY_AUTO_GRID) za `STDistance`.
- **FULL-TEXT** katalog `RetroFitKatalog` (srpski word breaker LCID 2074, `ACCENT_SENSITIVITY = OFF`) nad `Oglasi(Naslov, Opis)` i `ZahteviTrazim(Naslov, Opis)`.
- Ukupno 19 filtriranih indeksa i 21 UNIQUE indeks (pored primarnih ključeva).

## 5. Napredne tehnike koje projekat koristi

| Tehnika | Gde | Zašto |
|---|---|---|
| **Full-Text Search** (`CONTAINS`) | `OglasRepository.Filtriraj` | Indeks reči umesto `LIKE '%x%'` (koji ne koristi indeks); prefiksna pretraga `"bicik*"`, srpski word breaker, bez osetljivosti na kvačice. Automatski LIKE fallback ako komponenta nije instalirana (`FullTextDetektor`). |
| **Prostorni tip `geography`** | `Gradovi.Lokacija`, pretraga po radijusu | `IsWithinDistance` → `STDistance` uz SPATIAL indeks. SRID 4326 (GPS). |
| **Rekurzivni CTE** | `KategorijaRepository` | "Elektronika" vraća i oglase iz svih podkategorija; podkategorija nasleđuje atribute nadređene. |
| **EAV model** | `DefinicijeAtributa` + `AtributiOglasa` | Različite kategorije imaju različite atribute bez menjanja šeme. |
| **Optimistička konkurentnost** (`rowversion`) | `Oglasi`, `Ponude`, `Rezervacije`, `Transakcije` | Dva istovremena prihvatanja ponude: drugi upis dobija `DbUpdateConcurrencyException` → 409. |
| **Filtrirani UNIQUE indeksi** | rezervacije, transakcije, ponude, prijave | Pravilo "najviše jedan aktivan red" koje dozvoljava istoriju zatvorenih redova. |
| **Atomični UPDATE** (`ExecuteUpdateAsync`) | prosek ocena, brojači pregleda, opoziv tokena | `UPDATE ... SET Prosek = (Prosek*N + @o)/(N+1), N = N+1` - u SQL-u SET koristi stare vrednosti, pa istovremene recenzije ne gube izmene (nema "lost update"). |
| **Jedna UPDATE naredba sa CASE** | promena redosleda slika | SQL Server proverava UNIQUE indeks na kraju naredbe, pa zamena mesta dve slike ne krši `UNIQUE (OglasId, Redosled)`. |
| **Eksplicitne transakcije** (`UTransakcijiAsync`) | prihvatanje ponude, kontraponuda, potvrda transakcije, recenzija | Više tabela i više `SaveChanges` u jednoj atomičnoj jedinici, uz execution strategy (retry). |
| **Denormalizacija** | `Korisnici.ProsecnaOcena`, `Oglasi.BrojOmiljenih/BrojPregleda` | Kartica oglasa prikazuje ocenu prodavca; bez ovoga bi svaka stranica rezultata radila `AVG` nad recenzijama. Trade-off: brže čitanje, malo skuplji upis (u istoj transakciji). |
| **Audit preko EF interceptora** | `AuditInterceptor` | Svaka izmena "važnih" entiteta → stare/nove vrednosti kao JSON, osetljiva polja maskirana. |
| **Prevođenje grešaka baze** | `UnitOfWork.PrevediSqlGresku` | SQL greške 2601/2627 (UNIQUE) i 547 (CHECK/FK) → razumljiva poruka i HTTP 409. |

## 6. Seed podaci

- U **migraciji** (`HasData`): 3 uloge, 152 mesta Srbije sa koordinatama (sedišta svih 145 gradova i opština + prigradska mesta Beograda; naziv sa srpskom kolacijom `Serbian_Latin_100_CI_AS`), 37 kategorija (10 glavnih + podkategorije), 28 definicija atributa.
- **Demo podaci** (samo razvoj/Docker, `DemoPodaciSeeder`): 6 korisnika, 18 oglasa, završena kupovina sa escrow-om i recenzijama, ponuda na čekanju, prijava, zahtev "Tražim", izdvojeni oglasi i jedan oglas star 61 dan (pozadinski posao ga automatski označava kao istekao). Svi demo podaci se prave kroz domenske metode.

## 7. Korisni SQL upiti za odbranu

```sql
-- Sva ograničenja jedne tabele
SELECT name, definition FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.Oglasi');
-- Filtrirani indeksi
SELECT OBJECT_NAME(object_id) Tabela, name, filter_definition FROM sys.indexes WHERE has_filter = 1;
-- Full-Text pretraga direktno
SELECT Id, Naslov FROM Oglasi WHERE CONTAINS((Naslov, Opis), N'"bicik*"');
-- Gradovi do 70 km od Beograda
DECLARE @bg geography = (SELECT Lokacija FROM Gradovi WHERE Id = 1);
SELECT Naziv, Lokacija.STDistance(@bg)/1000 AS Km FROM Gradovi WHERE Lokacija.STDistance(@bg) <= 70000 ORDER BY Km;
-- Pokušaj duplog prodavanja (baza odbija drugi red)
SELECT name, filter_definition FROM sys.indexes WHERE name = 'UX_Transakcije_OglasNeotkazana';
```
