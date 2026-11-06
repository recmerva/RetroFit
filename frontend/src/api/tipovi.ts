// TypeScript tipovi koji odgovaraju DTO-ima iz backend-a (RetroFit.Application/Features/*Dto.cs).

export type StatusOglasa = 'Nacrt' | 'Aktivan' | 'Rezervisan' | 'Prodat' | 'Istekao' | 'Uklonjen'
export type StanjeProizvoda = 'Novo' | 'KaoNovo' | 'Polovno' | 'ZaDelove'
export type TipOglasa = 'Prodaja' | 'Razmena' | 'Poklon'
export type NacinPreuzimanja = 'LicnoPreuzimanje' | 'Dostava'
export type StatusPonude = 'NaCekanju' | 'Prihvacena' | 'Odbijena' | 'Kontraponuda' | 'Istekla'
export type StatusRezervacije = 'Aktivna' | 'Realizovana' | 'Otkazana' | 'Istekla'
export type StatusTransakcije = 'UToku' | 'Zavrsena' | 'Otkazana'
export type StatusEscrowa = 'NijeKoriscen' | 'CekaUplatu' | 'Deponovano' | 'Isplaceno' | 'Vraceno'
export type TipAtributa = 'Tekst' | 'Broj' | 'DaNe' | 'Izbor'
export type StatusZahteva = 'Otvoren' | 'Ispunjen' | 'Zatvoren'
export type RazlogPrijave = 'Prevara' | 'ZabranjenSadrzaj' | 'PogresnaKategorija' | 'Duplikat' | 'Spam' | 'UvredljivSadrzaj' | 'Ostalo'
export type StatusPrijave = 'Otvorena' | 'Prihvacena' | 'Odbijena'
export type SortiranjeOglasa = 'Najnovije' | 'CenaRastuce' | 'CenaOpadajuce' | 'Popularnost' | 'Udaljenost'
export type KategorijaAudita = 'Podaci' | 'Bezbednost' | 'Moderacija' | 'Administracija'
export type Uloga = 'Korisnik' | 'Moderator' | 'Admin'

export interface Stranica<T> {
  stavke: T[]
  stranica: number
  velicinaStranice: number
  ukupnoStavki: number
  ukupnoStranica: number
  imaSledecu: boolean
  imaPrethodnu: boolean
}

export interface ProblemDetails {
  status: number
  title: string
  detail?: string
  kod?: string
  greske?: Record<string, string[]>
  traceId?: string
}

export interface KorisnikKratko {
  id: number
  prikaznoIme: string
  prosecnaOcena: number
  brojRecenzija: number
  telefonVerifikovan: boolean
}

export interface JavniProfil {
  id: number
  prikaznoIme: string
  gradNaziv?: string
  oKorisniku?: string
  prosecnaOcena: number
  brojRecenzija: number
  brojZavrsenihTransakcija: number
  telefonVerifikovan: boolean
  clanOdUtc: string
  brojAktivnihOglasa: number
  reputacija: number
  pouzdanProdavac: boolean
  bedzevi: string[]
}

export interface MojProfil {
  id: number
  email: string
  ime: string
  prezime: string
  prikaznoIme: string
  oKorisniku?: string
  gradId?: number
  gradNaziv?: string
  telefon?: string
  telefonVerifikovan: boolean
  prikaziTelefonKupcu: boolean
  uloge: Uloga[]
  prosecnaOcena: number
  brojRecenzija: number
  brojZavrsenihTransakcija: number
  kreiranoUtc: string
  reputacija: number
  pouzdanProdavac: boolean
  bedzevi: string[]
}

export interface AutentifikacijaOdgovor {
  accessToken: string
  accessTokenIsticeUtc: string
  korisnik: MojProfil
}

export interface Grad {
  id: number
  naziv: string
  postanskiBroj: string
  geografskaSirina: number
  geografskaDuzina: number
}

export interface Kategorija {
  id: number
  naziv: string
  slug: string
  ikonica?: string
  redosled: number
  aktivna: boolean
  roditeljId?: number
  brojAktivnihOglasa: number
  podkategorije: Kategorija[]
}

export interface DefinicijaAtributa {
  id: number
  kategorijaId: number
  naziv: string
  kljuc: string
  tip: TipAtributa
  obavezan: boolean
  opcije: string[]
  jedinicaMere?: string
  redosled: number
}

export interface OglasKartica {
  id: number
  naslov: string
  cena?: number
  tip: TipOglasa
  stanje: StanjeProizvoda
  status: StatusOglasa
  gradId: number
  gradNaziv: string
  kategorijaId: number
  kategorijaNaziv: string
  objavljenoUtc?: string
  isticeUtc?: string
  glavnaSlika?: string
  prodavac: KorisnikKratko
  jeIstaknut: boolean
  brojOmiljenih: number
  brojPregleda: number
  udaljenostKm?: number
  jeOmiljen: boolean
}

export interface Slika {
  id: number
  putanja: string
  redosled: number
}

export interface AtributVrednost {
  definicijaAtributaId: number
  naziv: string
  kljuc: string
  tip: TipAtributa
  vrednost: string
  jedinicaMere?: string
}

export interface OglasDetalji {
  id: number
  naslov: string
  opis: string
  cena?: number
  tip: TipOglasa
  stanje: StanjeProizvoda
  status: StatusOglasa
  zeljenaZamena?: string
  dostavaMoguca: boolean
  licnoPreuzimanjeMoguce: boolean
  gradId: number
  gradNaziv: string
  kategorijaId: number
  kategorijaNaziv: string
  nadredjenaKategorijaId?: number
  nadredjenaKategorijaNaziv?: string
  kreiranoUtc: string
  objavljenoUtc?: string
  isticeUtc?: string
  brojPregleda: number
  brojOmiljenih: number
  razlogUklanjanja?: string
  slike: Slika[]
  atributi: AtributVrednost[]
  prodavac: JavniProfil
  jeMoj: boolean
  jeOmiljen: boolean
  jeIstaknut: boolean
  telefonProdavca?: string
  mojRazgovorId?: number
}

export interface OglasPodaci {
  kategorijaId: number
  gradId: number
  naslov: string
  opis: string
  tip: TipOglasa
  cena?: number | null
  stanje: StanjeProizvoda
  dostavaMoguca: boolean
  licnoPreuzimanjeMoguce: boolean
  zeljenaZamena?: string | null
  atributi?: Record<number, string | null>
  objaviOdmah?: boolean
}

export interface SacuvanaPretraga {
  id: number
  naziv: string
  upit?: string
  kategorijaId?: number
  kategorijaNaziv?: string
  gradId?: number
  gradNaziv?: string
  cenaOd?: number
  cenaDo?: number
  stanje?: StanjeProizvoda
  tip?: TipOglasa
  obavestavaj: boolean
  kreiranoUtc: string
}

export interface OglasUkratko {
  id: number
  naslov: string
  cena?: number
  tip: TipOglasa
  status: StatusOglasa
  glavnaSlika?: string
  prodavacId: number
}

export interface RazgovorPregled {
  id: number
  oglas: OglasUkratko
  sagovornikId: number
  sagovornikIme: string
  jaSamKupac: boolean
  poslednjaPoruka?: string
  poslednjaPorukaUtc: string
  brojNeprocitanih: number
}

export interface Poruka {
  id: number
  razgovorId: number
  posiljalacId: number
  sadrzaj: string
  tip: 'Tekst' | 'BrzaPoruka' | 'Sistemska'
  poslatoUtc: string
  procitanoUtc?: string
  ponudaId?: number
}

export interface Ponuda {
  id: number
  oglasId: number
  oglasNaslov: string
  cenaOglasa?: number
  oglasSlika?: string
  razgovorId: number
  kupacId: number
  kupacIme: string
  prodavacId: number
  prodavacIme: string
  ponudioKorisnikId: number
  iznos: number
  napomena?: string
  status: StatusPonude
  kreiranoUtc: string
  isticeUtc: string
  odgovorenoUtc?: string
  prethodnaPonudaId?: number
}

export interface RazgovorDetalji {
  id: number
  oglas: OglasUkratko
  kupacId: number
  kupacIme: string
  prodavacId: number
  prodavacIme: string
  jaSamKupac: boolean
  kreiranoUtc: string
  ponude: Ponuda[]
  aktivnaRezervacijaId?: number
  telefonProdavca?: string
}

export interface BrzaPoruka {
  kljuc: string
  tekst: string
}

export interface Rezervacija {
  id: number
  oglas: OglasUkratko
  kupacId: number
  kupacIme: string
  prodavacId: number
  prodavacIme: string
  ponudaId?: number
  dogovorenaCena?: number
  status: StatusRezervacije
  kreiranoUtc: string
  isticeUtc: string
  zavrsenoUtc?: string
  razlogOtkazivanja?: string
  transakcijaId?: number
}

export interface Placanje {
  id: number
  tip: 'EscrowUplata' | 'EscrowIsplata' | 'EscrowPovracaj' | 'Isticanje'
  status: 'Uspesno' | 'Neuspesno'
  iznos: number
  valuta: string
  referencaProcesora: string
  opis: string
  kreiranoUtc: string
}

export interface Transakcija {
  id: number
  oglas: OglasUkratko
  rezervacijaId: number
  kupacId: number
  kupacIme: string
  prodavacId: number
  prodavacIme: string
  cena?: number
  nacinPreuzimanja: NacinPreuzimanja
  adresaDostave?: string
  status: StatusTransakcije
  statusEscrowa: StatusEscrowa
  kreiranoUtc: string
  kupacPotvrdioUtc?: string
  prodavacPotvrdioUtc?: string
  zavrsenoUtc?: string
  otkazanoUtc?: string
  razlogOtkazivanja?: string
  kupacOstavioRecenziju: boolean
  prodavacOstavioRecenziju: boolean
  placanja: Placanje[]
  telefonDrugeStrane?: string
}

export interface Recenzija {
  id: number
  transakcijaId: number
  ocenjivacId: number
  ocenjivacIme: string
  ocenjeniKorisnikId: number
  ocena: number
  komentar?: string
  kreiranoUtc: string
  oglasNaslov: string
  ulogaOcenjivaca: string
}

export interface SazetakOcena {
  prosecnaOcena: number
  brojRecenzija: number
  raspodelaOcena: Record<string, number>
}

export interface Zahtev {
  id: number
  naslov: string
  opis: string
  kategorijaId: number
  kategorijaNaziv: string
  gradId?: number
  gradNaziv?: string
  maksimalnaCena?: number
  status: StatusZahteva
  kreiranoUtc: string
  korisnikId: number
  korisnikIme: string
  brojOdgovora: number
}

export interface ZahtevDetalji {
  zahtev: Zahtev
  odgovori: { id: number; oglas: OglasKartica; poruka?: string; kreiranoUtc: string }[]
  jeMoj: boolean
}

export interface Obavestenje {
  id: number
  tip: string
  naslov: string
  sadrzaj: string
  link?: string
  kreiranoUtc: string
  procitanoUtc?: string
}

export interface Brojaci {
  neprocitanaObavestenja: number
  neprocitanePoruke: number
  ponudeNaCekanju: number
}

export interface Prijava {
  id: number
  oglasId: number
  oglasNaslov: string
  oglasStatus: StatusOglasa
  prodavacId: number
  prodavacIme: string
  prijavioKorisnikId: number
  prijavioIme: string
  razlog: RazlogPrijave
  opis?: string
  status: StatusPrijave
  kreiranoUtc: string
  obradioIme?: string
  obradjenoUtc?: string
  napomenaModeratora?: string
  brojOtvorenihPrijavaZaOglas: number
}

export interface KorisnikAdmin {
  id: number
  email: string
  imePrezime: string
  uloge: Uloga[]
  kreiranoUtc: string
  poslednjaPrijavaUtc?: string
  blokiran: boolean
  obrisan: boolean
  brojOglasa: number
}

export interface BlokadaKorisnika {
  id: number
  korisnikId: number
  korisnikIme: string
  korisnikEmail: string
  blokiraoIme: string
  razlog: string
  pocetakUtc: string
  krajUtc?: string
  ukinutoUtc?: string
  aktivna: boolean
}

export interface OtisakUredjaja {
  id: number
  otisakSkraceno: string
  opisKlijenta?: string
  prviPutVidjenUtc: string
  poslednjiPutVidjenUtc: string
  brojZahteva: number
  brojRegistracija: number
  najveciRizik: number
  blokiran: boolean
}

export interface BlokadaUredjaja {
  id: number
  otisakUredjajaId: number
  otisakSkraceno: string
  razlog: string
  automatska: boolean
  pocetakUtc: string
  krajUtc?: string
  ukinutoUtc?: string
}

export interface StavkaStatistike {
  oznaka: string
  vrednost: number
}

export interface Statistika {
  ukupnoKorisnika: number
  noviKorisnici7Dana: number
  aktivniOglasi: number
  rezervisaniOglasi: number
  prodatiOglasi: number
  noviOglasi7Dana: number
  zavrseneTransakcije: number
  ukupanPromet: number
  prometEscrow: number
  prihodOdIsticanja: number
  otvorenePrijave: number
  aktivneBlokadeKorisnika: number
  aktivneBlokadeUredjaja: number
  bezbednosniDogadjaji24h: number
  prosecnaOcenaPlatforme: number
  oglasiPoKategorijama: StavkaStatistike[]
  transakcijePoDanima: StavkaStatistike[]
}

export interface AuditZapis {
  id: number
  kategorija: KategorijaAudita
  akcija: string
  nazivEntiteta?: string
  entitetId?: string
  korisnikId?: number
  korisnikEmail?: string
  stareVrednosti?: string
  noveVrednosti?: string
  ipAdresa?: string
  korelacioniId?: string
  kreiranoUtc: string
}

export interface IstaknutiOglas {
  id: number
  oglasId: number
  oglasNaslov: string
  tip: 'Izdvojen' | 'Podignut'
  pocetakUtc: string
  krajUtc: string
  otkazanoUtc?: string
  kreiraoIme: string
  placeniIznos?: number
}
