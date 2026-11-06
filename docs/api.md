# API dokumentacija

Interaktivna dokumentacija (Swagger / OpenAPI, sa primerima zahteva i odgovora): **http://localhost:5029/swagger** (lokalno) ili **http://localhost:5000/swagger** (Docker).
Za zaštićene endpointe: prijavite se (`POST /api/v1/autentifikacija/prijava`), kopirajte `accessToken`, kliknite **Authorize**.

Sve rute počinju sa `/api/v1`. Enumi se šalju i vraćaju kao tekst (`"Aktivan"`, `"Prodaja"`).

## Format grešaka (ProblemDetails, RFC 7807)

```json
{
  "type": "https://retrofit.rs/greske/validacija",
  "title": "Neispravni podaci",
  "status": 400,
  "detail": "Jedno ili više polja nije ispravno.",
  "instance": "/api/v1/autentifikacija/registracija",
  "kod": "validacija",
  "greske": { "lozinka": ["Lozinka mora imati najmanje 8 karaktera."] },
  "traceId": "9ccd0e6ae97d47548e6d7d6856a04a1e"
}
```

| Status | `kod` | Kada |
|---|---|---|
| 400 | `validacija` | FluentValidation ili neispravan JSON |
| 401 | `neovlascen` | nema/istekao access token, pogrešna lozinka |
| 403 | `zabranjeno`, `blokiran`, `uredjaj_blokiran` | nema ovlašćenje / blokiran nalog ili uređaj |
| 404 | `nije_pronadjeno` | resurs ne postoji (ili nije vaš) |
| 409 | `konflikt` | UNIQUE/RowVersion: već rezervisano, dupla recenzija, isti email... |
| 422 | npr. `nedozvoljen_prelaz`, `ponuda_istekla` | prekršeno poslovno pravilo |
| 428 | `potreban_izazov` | Bot Detection traži rešen JS izazov |
| 429 | `previse_zahteva`, `spam`, `privremeno_blokirano` | rate limit (zaglavlje `Retry-After`) |

## Endpointi

### Autentifikacija i profil
| Metoda | Ruta | Opis |
|---|---|---|
| POST | `/autentifikacija/registracija` | registracija (traži JS izazov u režimu Zastita) |
| POST | `/autentifikacija/prijava` | prijava → access token + refresh cookie |
| POST | `/autentifikacija/osvezi` | rotacija refresh tokena |
| POST | `/autentifikacija/odjava` · `/odjava-svuda` | odjava (jedan / svi uređaji) |
| GET/PUT/DELETE | `/korisnici/ja` | moj profil / izmena / brisanje naloga |
| POST | `/korisnici/ja/lozinka` | promena lozinke |
| POST | `/korisnici/ja/telefon` · `/telefon/potvrda` | SMS kod / potvrda |
| GET | `/korisnici/{id}` · `/oglasi` · `/recenzije` · `/ocene` | javni profil, oglasi, recenzije, raspodela ocena |

### Katalog i oglasi
| Metoda | Ruta | Opis |
|---|---|---|
| GET | `/gradovi`, `/kategorije`, `/kategorije/{id}/atributi` | šifarnici |
| GET | `/oglasi?upit=&kategorijaId=&gradId=&cenaOd=&cenaDo=&stanja=&tip=&samoSaDostavom=&lat=&lng=&radijusKm=&sortiranje=&stranica=&velicina=` | pretraga |
| GET | `/oglasi/izdvojeni`, `/oglasi/{id}`, `/oglasi/moji` | izdvojeni, detalji, moji |
| POST/PUT/DELETE | `/oglasi`, `/oglasi/{id}` | kreiranje, izmena, brisanje |
| POST | `/oglasi/{id}/objava` · `/obnova` · `/prodat` | promene statusa |
| POST/DELETE/PUT | `/oglasi/{id}/slike`, `/slike/{slikaId}`, `/slike/redosled` | slike (multipart, max 10) |
| POST | `/oglasi/{id}/podizanje` | plaćeno podizanje (mock) |
| GET/PUT/DELETE | `/omiljeni`, `/omiljeni/{oglasId}` | lista želja |
| GET/POST/PUT/DELETE | `/sacuvane-pretrage` | sačuvane pretrage sa obaveštenjima |

### Komunikacija i trgovina
| Metoda | Ruta | Opis |
|---|---|---|
| GET/POST | `/razgovori` | moji razgovori / započni razgovor |
| GET | `/razgovori/{id}`, `/razgovori/{id}/poruke`, `/razgovori/brze-poruke` | detalji, istorija, brze poruke |
| POST | `/razgovori/{id}/poruke`, `/razgovori/{id}/procitano` | slanje, označavanje pročitanim |
| POST | `/oglasi/{id}/ponude` | ponuda cene |
| GET | `/ponude?kaoKupac=&status=` | moje ponude |
| POST | `/ponude/{id}/prihvati` · `/odbij` · `/kontraponuda` | odgovor na ponudu |
| POST | `/razgovori/{id}/rezervacija` | prodavac rezerviše za kupca |
| GET | `/rezervacije`, `/rezervacije/{id}` | rezervacije |
| POST | `/rezervacije/{id}/otkazi` · `/kupovina` | otkazivanje / pokretanje kupovine |
| GET | `/transakcije`, `/transakcije/{id}` | istorija i detalji sa plaćanjima |
| POST | `/transakcije/{id}/uplata` · `/potvrdi` · `/otkazi` · `/recenzija` | escrow, potvrde, otkazivanje, recenzija |
| GET/POST/PUT | `/zahtevi`, `/zahtevi/moji`, `/zahtevi/{id}`, `/zahtevi/{id}/zatvori`, `/zahtevi/{id}/odgovori` | "Tražim..." |
| GET/POST | `/obavestenja`, `/obavestenja/brojaci`, `/obavestenja/{id}/procitano`, `/obavestenja/procitano` | obaveštenja |

### Moderacija (Moderator, Admin) i administracija (Admin)
| Metoda | Ruta | Opis |
|---|---|---|
| POST | `/oglasi/{id}/prijava` | prijava oglasa (svaki korisnik) |
| GET/POST | `/moderacija/prijave`, `/moderacija/prijave/{id}/obrada` | obrada prijava |
| POST | `/moderacija/oglasi/{id}/uklanjanje` | uklanjanje oglasa |
| GET/POST/DELETE | `/moderacija/korisnici`, `/moderacija/korisnici/{id}/blokada`, `/moderacija/blokade` | korisnici i blokade |
| GET | `/admin/statistika`, `/admin/audit` | statistika, audit log |
| PUT | `/admin/korisnici/{id}/uloge` | uloge |
| GET/POST/DELETE | `/admin/istaknuti`, `/admin/oglasi/{id}/izdvajanje` | izdvajanje oglasa |
| GET/POST/DELETE | `/admin/uredjaji`, `/admin/uredjaji/{id}/blokada`, `/admin/uredjaji/blokade` | Bot Detection uređaji |
| POST/PUT/PATCH/DELETE | `/kategorije`, `/kategorije/{id}`, `/kategorije/{id}/aktivnost`, `/kategorije/{id}/atributi` | kategorije i atributi |

### Bezbednost i razvoj
| Metoda | Ruta | Opis |
|---|---|---|
| GET/POST | `/bezbednost/izazov` | JS proof-of-work izazov |
| GET | `/bezbednost/procena` | procena rizika (samo razvoj) |
| GET | `/razvoj/sms` | poruke mock SMS servisa (samo razvoj) |
| GET | `/health`, `/health/ready` | živost i spremnost (baza) |

## Primer: ceo tok kupovine

```http
POST /api/v1/oglasi/3/ponude            { "iznos": 12000 }                         -> 201 ponuda #4
POST /api/v1/ponude/4/kontraponuda      { "iznos": 14000 }   (prodavac)             -> 201 ponuda #5
POST /api/v1/ponude/5/prihvati                               (kupac)                -> 200 rezervacija #2, oglas Rezervisan
POST /api/v1/rezervacije/2/kupovina     { "nacinPreuzimanja": "Dostava", "adresaDostave": "...", "koristiEscrow": true }
                                                                                    -> 201 transakcija #2, escrow CekaUplatu
POST /api/v1/transakcije/2/uplata       { "kljucIdempotentnosti": "uuid" }          -> escrow Deponovano
POST /api/v1/transakcije/2/potvrdi                           (prodavac)             -> čeka kupca
POST /api/v1/transakcije/2/potvrdi                           (kupac)                -> Zavrsena, escrow Isplaceno, oglas Prodat
POST /api/v1/transakcije/2/recenzija    { "ocena": 5, "komentar": "Super" }        -> 201 (drugi put -> 409)
```
