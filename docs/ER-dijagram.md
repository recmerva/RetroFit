# ER dijagram baze RetroFit

Baza ima **29 tabela**. Dijagram je u Mermaid formatu (GitHub ga prikazuje automatski; lokalno u VS Code uz ekstenziju *Markdown Preview Mermaid Support*).
Oznake: `PK` primarni ključ, `FK` strani ključ, `UK` jedinstveno (UNIQUE) ograničenje. Detaljna objašnjenja su u [baza-podataka.md](baza-podataka.md).

## 1. Korisnici i bezbednost

```mermaid
erDiagram
    Korisnici ||--o{ KorisnikUloge : "ima"
    Uloge ||--o{ KorisnikUloge : "dodeljena"
    Korisnici ||--o{ RefreshTokeni : "poseduje"
    Korisnici ||--o{ VerifikacijeKorisnika : "verifikuje"
    Korisnici ||--o{ BlokiraniKorisnici : "blokiran"
    Gradovi |o--o{ Korisnici : "živi u"
    Korisnici |o--o{ OtisciUredjaja : "poslednji koristio"
    OtisciUredjaja ||--o{ BlokiraniUredjaji : "blokiran"
    Korisnici |o--o{ AuditZapisi : "akter"

    Korisnici {
        int Id PK
        nvarchar Email
        nvarchar NormalizovanEmail UK
        varchar LozinkaHash "PBKDF2, nikad plain-text"
        nvarchar Ime
        nvarchar Prezime
        varchar Telefon UK "filtrirani: NOT NULL i nije obrisan"
        datetime2 TelefonVerifikovanUtc
        bit PrikaziTelefonKupcu
        int GradId FK
        decimal ProsecnaOcena "denormalizovano 3,2"
        int BrojRecenzija
        int BrojZavrsenihTransakcija
        bit Obrisan "meko brisanje + anonimizacija"
        datetime2 KreiranoUtc
        datetime2 IzmenjenoUtc
    }
    Uloge {
        int Id PK
        nvarchar Naziv UK
    }
    KorisnikUloge {
        int KorisnikId PK, FK
        int UlogaId PK, FK
        datetime2 DodeljenoUtc
    }
    RefreshTokeni {
        int Id PK
        int KorisnikId FK
        char TokenHash UK "SHA-256"
        uniqueidentifier PorodicaId "rotacija"
        datetime2 IsticeUtc
        datetime2 OpozvanoUtc
        char ZamenjenHashom
    }
    VerifikacijeKorisnika {
        int Id PK
        int KorisnikId FK
        varchar Tip
        char KodHash
        int BrojPokusaja "max 5"
        datetime2 IsticeUtc
    }
    BlokiraniKorisnici {
        int Id PK
        int KorisnikId FK
        int BlokiraoKorisnikId FK
        nvarchar Razlog
        datetime2 KrajUtc "NULL = trajno"
        datetime2 UkinutoUtc
    }
    OtisciUredjaja {
        int Id PK
        char OtisakHash UK "HMAC-SHA256"
        nvarchar OpisKlijenta
        bigint BrojZahteva
        int BrojRegistracija
        int NajveciRizik "0-100"
        int PoslednjiKorisnikId FK
    }
    BlokiraniUredjaji {
        int Id PK
        int OtisakUredjajaId FK
        bit Automatska
        datetime2 KrajUtc
    }
    AuditZapisi {
        int Id PK
        varchar Kategorija
        nvarchar Akcija
        nvarchar NazivEntiteta
        nvarchar EntitetId
        int KorisnikId FK
        nvarchar StareVrednosti "JSON, CHECK ISJSON"
        nvarchar NoveVrednosti "JSON, CHECK ISJSON"
        varchar IpAdresa
        datetime2 KreiranoUtc
    }
```

## 2. Katalog i oglasi

```mermaid
erDiagram
    Kategorije |o--o{ Kategorije : "podkategorija"
    Kategorije ||--o{ DefinicijeAtributa : "definiše"
    Kategorije ||--o{ Oglasi : "sadrži"
    Gradovi ||--o{ Oglasi : "lokacija"
    Korisnici ||--o{ Oglasi : "prodaje"
    Oglasi ||--o{ SlikeOglasa : "ima (max 10)"
    Oglasi ||--o{ AtributiOglasa : "ima"
    DefinicijeAtributa ||--o{ AtributiOglasa : "tip vrednosti"
    Oglasi ||--o{ OmiljeniOglasi : "omiljen"
    Korisnici ||--o{ OmiljeniOglasi : "čuva"
    Korisnici ||--o{ SacuvanePretrage : "čuva"
    Oglasi ||--o{ IstaknutiOglasi : "istaknut"
    Placanja |o--o| IstaknutiOglasi : "plaćeno podizanje"

    Gradovi {
        int Id PK
        nvarchar Naziv UK
        varchar PostanskiBroj
        float GeografskaSirina
        float GeografskaDuzina
        geography Lokacija "SPATIAL indeks"
    }
    Kategorije {
        int Id PK
        nvarchar Naziv
        varchar Slug UK
        int RoditeljId FK "samoreferenca"
        int Redosled
        bit Aktivna
    }
    DefinicijeAtributa {
        int Id PK
        int KategorijaId FK
        varchar Kljuc "UK sa KategorijaId"
        varchar Tip "Tekst, Broj, DaNe, Izbor"
        bit Obavezan
        nvarchar Opcije
    }
    Oglasi {
        int Id PK
        int ProdavacId FK
        int KategorijaId FK
        int GradId FK
        nvarchar Naslov "FULL-TEXT"
        nvarchar Opis "FULL-TEXT"
        decimal Cena "CHECK po tipu"
        varchar Tip "Prodaja, Razmena, Poklon"
        varchar Stanje
        varchar Status "mašina stanja"
        bit DostavaMoguca
        bit LicnoPreuzimanjeMoguce
        datetime2 ObjavljenoUtc
        datetime2 IsticeUtc "objava + 60 dana"
        int BrojPregleda
        int BrojOmiljenih
        bit Obrisan
        rowversion RowVersion
    }
    SlikeOglasa {
        int Id PK
        int OglasId FK
        nvarchar Putanja
        int Redosled "UK sa OglasId, 0-9"
    }
    AtributiOglasa {
        int OglasId PK, FK
        int DefinicijaAtributaId PK, FK
        nvarchar Vrednost
    }
    OmiljeniOglasi {
        int KorisnikId PK, FK
        int OglasId PK, FK
        datetime2 DodatoUtc
    }
    SacuvanePretrage {
        int Id PK
        int KorisnikId FK
        nvarchar Upit
        int KategorijaId FK
        int GradId FK
        decimal CenaOd
        decimal CenaDo
        bit Obavestavaj
        datetime2 PoslednjaProveraUtc
    }
    IstaknutiOglasi {
        int Id PK
        int OglasId FK
        varchar Tip "Izdvojen, Podignut"
        datetime2 PocetakUtc
        datetime2 KrajUtc
        int PlacanjeId FK "UK filtrirani"
    }
```

## 3. Komunikacija, rezervacije i transakcije

```mermaid
erDiagram
    Oglasi ||--o{ Razgovori : "tema"
    Korisnici ||--o{ Razgovori : "kupac / prodavac"
    Razgovori ||--o{ Poruke : "sadrži"
    Razgovori ||--o{ Ponude : "pregovor"
    Ponude |o--o{ Ponude : "kontraponuda"
    Oglasi ||--o{ Rezervacije : "rezervisan"
    Ponude |o--o| Rezervacije : "prihvaćena"
    Rezervacije ||--o| Transakcije : "postaje (1:1)"
    Transakcije ||--o{ Placanja : "escrow knjiženja"
    Transakcije ||--o{ Recenzije : "najviše 2"
    Korisnici ||--o{ Recenzije : "ocenjivač / ocenjeni"
    Korisnici ||--o{ Obavestenja : "prima"

    Razgovori {
        int Id PK
        int OglasId FK "UK sa KupacId"
        int KupacId FK
        int ProdavacId FK "CHECK <> KupacId"
        datetime2 PoslednjaPorukaUtc
    }
    Poruke {
        int Id PK
        int RazgovorId FK
        int PosiljalacId FK
        nvarchar Sadrzaj
        varchar Tip
        datetime2 ProcitanoUtc
        int PonudaId FK
    }
    Ponude {
        int Id PK
        int OglasId FK
        int RazgovorId FK "UK filtrirani: jedna NaCekanju"
        int KupacId FK
        int PonudioKorisnikId FK
        decimal Iznos "CHECK > 0"
        varchar Status
        datetime2 IsticeUtc "48h"
        int PrethodnaPonudaId FK
        rowversion RowVersion
    }
    Rezervacije {
        int Id PK
        int OglasId FK "UK filtrirani: jedna Aktivna"
        int KupacId FK "NOT NULL"
        int ProdavacId FK
        int PonudaId FK "UK filtrirani"
        decimal DogovorenaCena
        varchar Status
        datetime2 IsticeUtc "72h"
        rowversion RowVersion
    }
    Transakcije {
        int Id PK
        int OglasId FK "UK filtrirani: Status <> Otkazana"
        int RezervacijaId FK "UK"
        int KupacId FK
        int ProdavacId FK
        decimal Cena
        varchar NacinPreuzimanja
        nvarchar AdresaDostave "privatno"
        varchar Status
        varchar StatusEscrowa
        datetime2 KupacPotvrdioUtc
        datetime2 ProdavacPotvrdioUtc
        rowversion RowVersion
    }
    Placanja {
        int Id PK
        int KorisnikId FK
        int TransakcijaId FK
        varchar Tip "EscrowUplata, EscrowIsplata, EscrowPovracaj, Isticanje"
        varchar Status
        decimal Iznos
        varchar ReferencaProcesora UK
        varchar KljucIdempotentnosti UK "filtrirani"
    }
    Recenzije {
        int Id PK
        int TransakcijaId FK "UK sa OcenjivacId"
        int OcenjivacId FK
        int OcenjeniKorisnikId FK
        tinyint Ocena "CHECK 1-5"
        nvarchar Komentar
    }
    Obavestenja {
        int Id PK
        int KorisnikId FK
        varchar Tip
        nvarchar Naslov
        nvarchar Link
        datetime2 ProcitanoUtc
    }
```

## 4. Zahtevi "Tražim..." i moderacija

```mermaid
erDiagram
    Korisnici ||--o{ ZahteviTrazim : "traži"
    Kategorije ||--o{ ZahteviTrazim : "u kategoriji"
    ZahteviTrazim ||--o{ OdgovoriNaZahteve : "odgovori"
    Oglasi ||--o{ OdgovoriNaZahteve : "ponuđen"
    Oglasi ||--o{ Prijave : "prijavljen"
    Korisnici ||--o{ Prijave : "prijavljuje / obrađuje"

    ZahteviTrazim {
        int Id PK
        int KorisnikId FK
        nvarchar Naslov "FULL-TEXT"
        nvarchar Opis "FULL-TEXT"
        int KategorijaId FK
        int GradId FK
        decimal MaksimalnaCena
        varchar Status
    }
    OdgovoriNaZahteve {
        int Id PK
        int ZahtevId FK "UK sa OglasId"
        int OglasId FK
        int OdgovorioKorisnikId FK
        nvarchar Poruka
    }
    Prijave {
        int Id PK
        int OglasId FK "UK filtrirani sa PrijavioKorisnikId"
        int PrijavioKorisnikId FK
        varchar Razlog
        varchar Status
        int ObradioModeratorId FK
        nvarchar NapomenaModeratora
    }
```
