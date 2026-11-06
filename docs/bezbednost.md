# Bezbednost

## Autentifikacija i sesije

- **Lozinke:** `PasswordHasher<T>` (ASP.NET Core Identity V3): PBKDF2-HMAC-SHA512, 100.000 iteracija, nasumična so od 128 bita. Pravilo: najmanje 8 znakova, slovo + cifra.
- **Access token:** JWT HS256, 15 minuta, sadrži samo `sub` (Id), ime za prikaz i uloge - bez emaila i telefona. Validiraju se izdavač, publika, potpis, rok i algoritam.
- **Refresh token:** 64 kriptografski nasumična bajta, 14 dana, u **httpOnly + SameSite=Strict** cookie-ju sa putanjom `/api/v1/autentifikacija` (JavaScript ga ne vidi - XSS ga ne može ukrasti; drugi sajtovi ga ne mogu poslati - CSRF). U bazi samo **SHA-256 heš**.
- **Rotacija:** svako osvežavanje opoziva stari token (`ZamenjenHashom`) i izdaje novi u istoj porodici.
- **Detekcija ponovne upotrebe (revoke list):** ako stigne već opozvan token, opoziva se **cela porodica** i beleži bezbednosni događaj - napadač i žrtva moraju ponovo da se prijave.
- **Tolerancija za istovremene zahteve (30 s):** dva taba ili dva paralelna zahteva pri učitavanju šalju isti kolačić; prvi rotira token, drugi stiže milisekundama kasnije sa "opozvanim" tokenom. Takav zahtev se ne tretira kao krađa ako je token rotiran (razlog `Rotacija`) pre najviše 30 s **i** njegov naslednik je još važeći - izdaje se još jedan token u istoj porodici. Posle 30 s, ili ako je naslednik opozvan (odjava, ponovna upotreba), važi puna detekcija. Kompromis je svestan (isti pristup kao "reuse interval" kod Auth0): napadač bi morao da upotrebi ukradeni token u tih 30 s. Frontend dodatno spaja sva istovremena osvežavanja u jedan zahtev (`osveziSesiju` u `api/klijent.ts`).
- Promena lozinke, promena uloga, blokada i brisanje naloga opozivaju sve refresh tokene korisnika.
- Pogrešan email i pogrešna lozinka daju istu poruku, a za nepostojeći nalog se proverava lažni heš (isto vreme odgovora - zaštita od enumeracije naloga).
- **SMS verifikacija:** šestocifren kod iz kriptografskog generatora, u bazi kao heš, rok 10 min, najviše 5 pokušaja, najviše 3 koda u 15 minuta (mock SMS servis).

## Autorizacija

| Uloga | Ovlašćenja |
|---|---|
| Korisnik | sopstveni oglasi, poruke, ponude, kupovine, recenzije, prijave |
| Moderator | + obrada prijava, uklanjanje oglasa, pretraga i blokiranje običnih korisnika |
| Admin | + kategorije i atributi, uloge, izdvajanje oglasa, blokada uređaja, statistika, audit log; jedini može da blokira moderatora; admin se ne može blokirati |

Politike `Moderacija` i `Administracija` na kontrolerima + provere vlasništva u handlerima (npr. tuđ oglas → 403, tuđ razgovor → 404 da se ne otkrije postojanje).

## Privatnost podataka

- Javni DTO-i ne sadrže email, telefon, adresu ni heševe.
- Telefon prodavca vidi **samo kupac sa aktivnom ili realizovanom rezervacijom** (nastaje iz prihvaćene ponude), i to samo ako je prodavac verifikovao broj i uključio opciju prikaza.
- Adresa dostave je vidljiva samo učesnicima transakcije.
- Audit log maskira lozinke, emailove, telefone, adrese i tokene; sadržaj poruka se ne audituje.
- Brisanje naloga = anonimizacija (red ostaje zbog integriteta istorije transakcija i recenzija).
- IP adrese u logovima Bot Detection-a su maskirane.

## Ulaz i izlaz

- FluentValidation za sve komande + domenske invarijante + CHECK ograničenja u bazi (tri sloja).
- JSON ograničen na dubinu 32; straničenje ograničeno na najviše 50 stavki.
- Full-Text upit se pravi samo od slova i cifara (nema "ubacivanja" FTS operatora); sav SQL je parametrizovan.
- Slike: provera po potpisu fajla (magic bytes), ne po ekstenziji; samo JPEG/PNG/WebP do 5 MB; ime fajla generiše server (GUID); brisanje proverava da je putanja unutar foldera.
- Bezbednosna zaglavlja: `X-Content-Type-Options`, `X-Frame-Options: DENY`, `Referrer-Policy`, `Permissions-Policy`.
- Greške: klijent nikad ne dobija stack trace; dobija ProblemDetails sa `traceId` za pretragu logova.
- Docker: API radi kao neprivilegovan korisnik; tajne kroz promenljive okruženja (`.env`), ne u kodu.

## Zavisnosti

Verzije su izabrane zbog licenci: MediatR 12.x, AutoMapper 13.x i FluentAssertions 7.x su poslednje verzije sa besplatnom (Apache/MIT) licencom.

**Poznata ranjivost GHSA-rvv3-g6hj-g44x (AutoMapper < 15.1.1):** DoS (stack overflow) pri mapiranju duboko ugnježdenog grafa objekata koji kontroliše napadač. Ispravka postoji samo u komercijalnim verzijama. U ovom projektu nije iskoristiva:
1. AutoMapper mapira samo entitete iz baze (plitki grafovi, pod našom kontrolom) u DTO-e - nikada korisnički ulaz;
2. JSON ulaz je ograničen na dubinu 32 (`JsonSerializerOptions.MaxDepth`);
3. sve mape imaju `MaxDepth(3)`.
Upozorenje je zato utišano samo za taj advisory (`NuGetAuditSuppress` u `Directory.Build.props`). U produkciji bi preporuka bila nabavka licence za AutoMapper 15+ ili prelazak na ručno mapiranje.
