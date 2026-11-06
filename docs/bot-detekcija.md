# Bot Detection / Fingerprinting middleware

Sopstveni middleware projekta: `backend/RetroFit.API/Middleware/BotDetekcija/`.

| Fajl | Uloga |
|---|---|
| `BotDetekcijaMiddleware.cs` | Tok obrade zahteva i odluka (propusti / izazov / ograniči / blokiraj) |
| `KlasifikacijaRuta.cs` | Grupe ruta (Registracija, Prijava, Poruke, Pisanje, Pretraga, PregledOglasa, Opste) |
| `KlijentskiSignali.cs` | Parsiranje signala sa frontenda, HMAC otisak, gruba porodica klijenta, maskiranje IP-a |
| `PracenjePonasanja.cs` | Klizni prozori u memoriji, privremene blokade, statistika za bazu |
| `AnalizatorRizika.cs` | Čista funkcija: skor 0-100 + čitljivi razlozi |
| `JsIzazovServis.cs` | JavaScript proof-of-work izazov i tokeni |
| `BotDetekcijaSinhronizacija.cs` | Periodičan upis u `OtisciUredjaja`, `BlokiraniUredjaji`, `AuditZapisi` |

## Tok za jedan zahtev

```
zahtev ─► klasifikacija rute ─► (Ignorisi: swagger/uploads/health ─► dalje)
        ─► otisak = HMAC(tajni ključ, pregledač|jezik|ekran|zona|platforma|DPR|TLS)
        ─► ključ klijenta = korisnik (ako je prijavljen) ili otisak
        ─► trajno blokiran uređaj?          ─► 403
        ─► privremeno blokiran (klijent/IP)? ─► 429 + Retry-After
        ─► klizni prozor po grupi ruta (+ po IP-u za prijavu/registraciju)
        ─► za poruke: heš normalizovanog teksta (spam)
        ─► AnalizatorRizika ─► skor + razlozi
        ─► odluka ─► propusti | 428 (JS izazov) | 429 (limit) | 429 (spam / blokada)
```

## Signali i poeni

| Signal | Poeni |
|---|---|
| Nema User-Agent | +40 |
| User-Agent alata (curl, python-requests, Selenium, HeadlessChrome, bot/crawler...) | +35 |
| Nema Accept-Language | +15 |
| Nema Accept | +5 |
| Nema klijentskih signala (naš frontend ih uvek šalje) | +10 |
| `navigator.webdriver = true` | +40 |
| Nedoslednost (mobilni UA, desktop ekran bez dodira) | +10 |
| Sumnjiv TLS otisak (samo od pouzdanog proxy-ja) | +30 |
| Prekoračen limit grupe / blizu limita | +30 / +10 |
| Scraping: > 60 različitih oglasa za 5 min | +30 |
| Enumeracija: ≥ 8 oglasa otvorenih redom po ID-ju | +35 |
| Robotski ritam: ≥ 15 zahteva, prosečan razmak < 5 s, koeficijent varijacije < 0.15 | +25 |
| Spam: ista poruka u ≥ 3 različita razgovora za 10 min | +40 |
| > 20 odgovora 404 za 5 min | +20 |
| Rešen JS izazov | do -30 |

**Odluka** (`BotDetekcijaMiddleware.Odluci`, pokrivena testovima):
1. spam → privremena blokada; 2. rizik ≥ 85 → privremena blokada (15 min); 3. prekoračen limit → 429;
4. registracija bez rešenog izazova → 428; 5. osetljiva ruta (prijava, registracija, poruke, izmene) i rizik ≥ 50 → 428; inače propusti.
Posle 3 privremene blokade u 24h uređaj se **automatski blokira u bazi** na 24h (`BlokiraniUredjaji`, `Automatska = 1`).

## Limiti (podrazumevano, menjaju se u `appsettings.json`)

| Grupa | Limit |
|---|---|
| Registracija | 3 / sat po klijentu (x3 po IP-u) - zaštita od masovnog pravljenja naloga |
| Prijava | 10 / 5 min po klijentu (x3 po IP-u) - zaštita od pogađanja lozinke |
| Poruke | 20 / min |
| Pisanje (POST/PUT/DELETE) | 60 / min |
| Pretraga | 90 / min - zaštita od scraping-a |
| Pregled oglasa | 120 / min |
| Ostalo | 300 / min |

## JavaScript izazov (umesto CAPTCHA)

1. `GET /api/v1/bezbednost/izazov` → `{ nonce, tezina, isticeUnix, potpis }` (HMAC potpis, server ne pamti izdate izazove).
2. Pregledač traži broj X tako da `SHA-256("nonce:X")` počinje sa `tezina` nultih bitova (Web Crypto API; težina 16 ≈ 65.000 heširanja ≈ 1 s).
3. `POST /api/v1/bezbednost/izazov` sa rešenjem → token vezan za **otisak uređaja**, važi 30 min; šalje se u `X-Challenge-Token`.
4. Replay zaštita: isti nonce se prihvata samo jednom. Poređenje potpisa je u konstantnom vremenu.

Frontend sve ovo radi automatski: na odgovor **428** reši izazov i ponovi zahtev (`frontend/src/api/klijent.ts`).

**Iskreno ograničenje:** proof-of-work ne dokazuje da je klijent čovek - odlučan napadač može da ga reši. On poskupljuje masovnu automatizaciju (svaka registracija košta CPU vreme) i zato je jedan sloj od više.

## Privatnost

- Ne čuvaju se sirovi signali: samo **HMAC-SHA256** heš (bez tajnog ključa ne može se ni "pogađati").
- Koriste se samo **grubi** signali (rezolucija, vremenska zona, jezik, platforma) - bez canvas/audio/font otisaka.
- IP adresa **nije deo otiska** (NAT, mobilne mreže) i u logovima je **maskirana** (IPv4 /24, IPv6 /48).
- Umesto punog User-Agent-a čuva se gruba porodica ("Chrome / Windows").
- Statistika se u bazu piše periodično (batch), a audit za jednog klijenta najviše jednom u 5 minuta.

## Šta middleware NE tvrdi (važno za odbranu)

- **IP adresa i User-Agent nisu pouzdani identifikatori uređaja.** IP dele mnogi korisnici (NAT, kampusi, mobilni operateri), menja se (VPN, mobilna mreža), a User-Agent se trivijalno lažira. Zato su to samo signali sa poenima, nikad jedini dokaz.
- **TLS otisak (JA3/JA4)** se računa iz TLS handshake-a. Kada TLS završava reverse proxy (nginx, load balancer, Cloudflare), ASP.NET Core aplikacija taj handshake **uopšte ne vidi** - dobija običan HTTP. Middleware zato prihvata otisak samo ako ga pouzdan proxy prosledi u zaglavlju `X-TLS-Fingerprint` (opcije `PrihvatiTlsOtisakOdProxyja` i `PouzdaniProxyji`). Ako zaglavlje stigne od bilo koga drugog, ignoriše se i loguje. Za stvarnu upotrebu potreban je proxy koji računa JA3/JA4 (npr. nginx sa odgovarajućim modulom ili CDN koji to podržava). Projekat ga ne uključuje.
- **IP iza proxy-ja**: `X-Forwarded-For` se prihvata samo od mreža iz `ReverseProxy:PouzdaneMreze` (ForwardedHeaders), inače bi napadač mogao da "izmisli" svoju IP adresu.
- Stanje je u memoriji jedne instance API-ja; za više instanci isti interfejs bi se implementirao preko Redis-a.

## Režimi rada

| Režim | Ponašanje | Gde |
|---|---|---|
| `Iskljuceno` | middleware se preskače | po potrebi |
| `SamoPracenje` | računa rizik i loguje "šta bi uradio", **nikad ne blokira** | razvoj (`appsettings.Development.json`) - legitimni zahtevi nisu ometeni |
| `Zastita` | puna zaštita | produkcija i Docker |

Za demonstraciju u razvoju: `dotnet run --launch-profile http -- --BotDetekcija:Rezim=Zastita`, a `GET /api/v1/bezbednost/procena` prikazuje rizik i razloge za trenutni zahtev (uključeno opcijom `PrikaziProcenu`, isključeno u produkciji jer bi botu otkrilo pravila).

## Kako demonstrirati

```bash
# 1) "Bot" - visok rizik i razlozi
curl -s -A "python-requests/2.31" http://localhost:5029/api/v1/bezbednost/procena
# 2) Registracija bez izazova (režim Zastita) -> 428 potreban_izazov
curl -s -X POST http://localhost:5029/api/v1/autentifikacija/registracija -H "Content-Type: application/json" -d "{}"
# 3) Rate limit: 100 brzih pretraga -> posle 90 dolazi 429 sa Retry-After
for i in $(seq 1 100); do curl -s -o /dev/null -w "%{http_code} " http://localhost:5029/api/v1/oglasi; done
```
U admin tabli (kartica "Uređaji") vide se otisci sa najvećim rizikom, a u audit logu događaji `BotDetekcija:*`.
