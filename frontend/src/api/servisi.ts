import { api } from './klijent'
import type * as T from './tipovi'

const d = <R>(p: Promise<{ data: R }>) => p.then((r) => r.data)

export interface ParametriPretrage {
  upit?: string
  kategorijaId?: number
  gradId?: number
  cenaOd?: number
  cenaDo?: number
  stanja?: T.StanjeProizvoda[]
  tip?: T.TipOglasa
  samoSaDostavom?: boolean
  lat?: number
  lng?: number
  radijusKm?: number
  sortiranje?: T.SortiranjeOglasa
  stranica?: number
  velicina?: number
}

// Niz se šalje kao ?stanja=A&stanja=B (format koji ASP.NET Core očekuje).
const serijalizuj = (p: object) => {
  const s = new URLSearchParams()
  Object.entries(p).forEach(([k, v]) => {
    if (v === undefined || v === null || v === '') return
    if (Array.isArray(v)) v.forEach((x) => s.append(k, String(x)))
    else s.append(k, String(v))
  })
  return s.toString()
}

export const autentifikacija = {
  prijava: (email: string, lozinka: string) => d(api.post<T.AutentifikacijaOdgovor>('/autentifikacija/prijava', { email, lozinka })),
  registracija: (podaci: { email: string; lozinka: string; ime: string; prezime: string; gradId?: number }) =>
    d(api.post<T.AutentifikacijaOdgovor>('/autentifikacija/registracija', podaci)),
  odjava: () => api.post('/autentifikacija/odjava', {}),
  odjavaSvuda: () => api.post('/autentifikacija/odjava-svuda', {})
}

export const korisnici = {
  ja: () => d(api.get<T.MojProfil>('/korisnici/ja')),
  azuriraj: (p: { ime: string; prezime: string; oKorisniku?: string; gradId?: number; prikaziTelefonKupcu: boolean }) =>
    d(api.put<T.MojProfil>('/korisnici/ja', p)),
  promeniLozinku: (trenutnaLozinka: string, novaLozinka: string) => api.post('/korisnici/ja/lozinka', { trenutnaLozinka, novaLozinka }),
  zatraziVerifikaciju: (telefon: string) => d(api.post<{ telefon: string; isticeUtc: string }>('/korisnici/ja/telefon', { telefon })),
  potvrdiTelefon: (kod: string) => d(api.post<T.MojProfil>('/korisnici/ja/telefon/potvrda', { kod })),
  obrisiNalog: (lozinka: string) => api.delete('/korisnici/ja', { data: { lozinka } }),
  javniProfil: (id: number) => d(api.get<T.JavniProfil>(`/korisnici/${id}`)),
  oglasi: (id: number, stranica = 1) => d(api.get<T.Stranica<T.OglasKartica>>(`/korisnici/${id}/oglasi?stranica=${stranica}`)),
  recenzije: (id: number, stranica = 1) => d(api.get<T.Stranica<T.Recenzija>>(`/korisnici/${id}/recenzije?stranica=${stranica}`)),
  ocene: (id: number) => d(api.get<T.SazetakOcena>(`/korisnici/${id}/ocene`)),
  razvojSms: () => d(api.get<{ brojTelefona: string; poruka: string; poslatoUtc: string }[]>('/razvoj/sms'))
}

export const katalog = {
  // "?v=152" menja adresu kada se spisak gradova promeni, pa pregledač ne koristi staru keširanu listu.
  gradovi: () => d(api.get<T.Grad[]>('/gradovi', { params: { v: 152 } })),
  kategorije: (ukljuciNeaktivne = false) => d(api.get<T.Kategorija[]>(`/kategorije?ukljuciNeaktivne=${ukljuciNeaktivne}`)),
  atributi: (kategorijaId: number) => d(api.get<T.DefinicijaAtributa[]>(`/kategorije/${kategorijaId}/atributi`)),
  sacuvajKategoriju: (id: number | null, p: { naziv: string; slug?: string; roditeljId?: number; ikonica?: string; redosled: number }) =>
    d(id ? api.put<T.Kategorija>(`/kategorije/${id}`, p) : api.post<T.Kategorija>('/kategorije', p)),
  aktivnostKategorije: (id: number, aktivna: boolean) => api.patch(`/kategorije/${id}/aktivnost`, { aktivna }),
  obrisiKategoriju: (id: number) => api.delete(`/kategorije/${id}`),
  dodajAtribut: (kategorijaId: number, p: { naziv: string; tip: T.TipAtributa; obavezan: boolean; opcije?: string[]; jedinicaMere?: string; redosled: number }) =>
    d(api.post<T.DefinicijaAtributa>(`/kategorije/${kategorijaId}/atributi`, p))
}

export const oglasi = {
  pretraga: (p: ParametriPretrage) => d(api.get<T.Stranica<T.OglasKartica>>(`/oglasi?${serijalizuj(p)}`)),
  izdvojeni: (maks = 8) => d(api.get<T.OglasKartica[]>(`/oglasi/izdvojeni?maks=${maks}`)),
  detalji: (id: number) => d(api.get<T.OglasDetalji>(`/oglasi/${id}`)),
  kreiraj: (p: T.OglasPodaci) => d(api.post<T.OglasDetalji>('/oglasi', p)),
  izmeni: (id: number, p: T.OglasPodaci) => d(api.put<T.OglasDetalji>(`/oglasi/${id}`, p)),
  obrisi: (id: number) => api.delete(`/oglasi/${id}`),
  objavi: (id: number) => d(api.post<T.OglasDetalji>(`/oglasi/${id}/objava`)),
  obnovi: (id: number) => d(api.post<T.OglasDetalji>(`/oglasi/${id}/obnova`)),
  prodat: (id: number) => d(api.post<T.OglasDetalji>(`/oglasi/${id}/prodat`)),
  dodajSlike: (id: number, fajlovi: File[]) => {
    const forma = new FormData()
    fajlovi.forEach((f) => forma.append('slike', f))
    return d(api.post<T.Slika[]>(`/oglasi/${id}/slike`, forma))
  },
  ukloniSliku: (id: number, slikaId: number) => api.delete(`/oglasi/${id}/slike/${slikaId}`),
  redosledSlika: (id: number, slikeIdURedosledu: number[]) => api.put(`/oglasi/${id}/slike/redosled`, { slikeIdURedosledu }),
  podigni: (id: number, brojDana: number) =>
    d(api.post<{ isticanjeId: number; krajUtc: string; placeniIznos: number; referencaPlacanja: string }>(`/oglasi/${id}/podizanje`, {
      brojDana,
      kljucIdempotentnosti: crypto.randomUUID()
    })),
  moji: (status?: T.StatusOglasa, stranica = 1) =>
    d(api.get<T.Stranica<T.OglasKartica>>(`/oglasi/moji?${serijalizuj({ status, stranica })}`)),
  prijavi: (id: number, razlog: T.RazlogPrijave, opis?: string) => api.post(`/oglasi/${id}/prijava`, { razlog, opis })
}

export const omiljeni = {
  lista: (stranica = 1) => d(api.get<T.Stranica<T.OglasKartica>>(`/omiljeni?stranica=${stranica}`)),
  dodaj: (oglasId: number) => api.put(`/omiljeni/${oglasId}`),
  ukloni: (oglasId: number) => api.delete(`/omiljeni/${oglasId}`)
}

export const sacuvanePretrage = {
  lista: () => d(api.get<T.SacuvanaPretraga[]>('/sacuvane-pretrage')),
  sacuvaj: (p: Omit<T.SacuvanaPretraga, 'id' | 'kreiranoUtc' | 'kategorijaNaziv' | 'gradNaziv'>) =>
    d(api.post<T.SacuvanaPretraga>('/sacuvane-pretrage', p)),
  obrisi: (id: number) => api.delete(`/sacuvane-pretrage/${id}`)
}

export const razgovori = {
  lista: (stranica = 1) => d(api.get<T.Stranica<T.RazgovorPregled>>(`/razgovori?stranica=${stranica}`)),
  zapocni: (oglasId: number, sadrzaj?: string, brzaPorukaKljuc?: string) =>
    d(api.post<T.RazgovorDetalji>('/razgovori', { oglasId, sadrzaj, brzaPorukaKljuc })),
  detalji: (id: number) => d(api.get<T.RazgovorDetalji>(`/razgovori/${id}`)),
  poruke: (id: number, stranica = 1) => d(api.get<T.Stranica<T.Poruka>>(`/razgovori/${id}/poruke?stranica=${stranica}&velicina=50`)),
  posalji: (id: number, sadrzaj?: string, brzaPorukaKljuc?: string) =>
    d(api.post<T.Poruka>(`/razgovori/${id}/poruke`, { sadrzaj, brzaPorukaKljuc })),
  procitano: (id: number) => api.post(`/razgovori/${id}/procitano`),
  brzePoruke: () => d(api.get<T.BrzaPoruka[]>('/razgovori/brze-poruke')),
  rezervisi: (razgovorId: number) => d(api.post<T.Rezervacija>(`/razgovori/${razgovorId}/rezervacija`))
}

export const ponude = {
  daj: (oglasId: number, iznos: number, napomena?: string) => d(api.post<T.Ponuda>(`/oglasi/${oglasId}/ponude`, { iznos, napomena })),
  lista: (kaoKupac?: boolean, status?: T.StatusPonude, stranica = 1) =>
    d(api.get<T.Stranica<T.Ponuda>>(`/ponude?${serijalizuj({ kaoKupac, status, stranica })}`)),
  prihvati: (id: number) => d(api.post<T.Rezervacija>(`/ponude/${id}/prihvati`)),
  odbij: (id: number) => d(api.post<T.Ponuda>(`/ponude/${id}/odbij`)),
  kontraponuda: (id: number, iznos: number, napomena?: string) => d(api.post<T.Ponuda>(`/ponude/${id}/kontraponuda`, { iznos, napomena }))
}

export const trgovina = {
  rezervacije: (status?: T.StatusRezervacije, stranica = 1) =>
    d(api.get<T.Stranica<T.Rezervacija>>(`/rezervacije?${serijalizuj({ status, stranica })}`)),
  otkaziRezervaciju: (id: number, razlog?: string) => d(api.post<T.Rezervacija>(`/rezervacije/${id}/otkazi`, { razlog })),
  zapocniKupovinu: (id: number, nacinPreuzimanja: T.NacinPreuzimanja, adresaDostave: string | undefined, koristiEscrow: boolean) =>
    d(api.post<T.Transakcija>(`/rezervacije/${id}/kupovina`, { nacinPreuzimanja, adresaDostave, koristiEscrow })),
  transakcije: (status?: T.StatusTransakcije, stranica = 1) =>
    d(api.get<T.Stranica<T.Transakcija>>(`/transakcije?${serijalizuj({ status, stranica })}`)),
  transakcija: (id: number) => d(api.get<T.Transakcija>(`/transakcije/${id}`)),
  uplati: (id: number) => d(api.post<T.Transakcija>(`/transakcije/${id}/uplata`, { kljucIdempotentnosti: crypto.randomUUID() })),
  potvrdi: (id: number) => d(api.post<T.Transakcija>(`/transakcije/${id}/potvrdi`)),
  otkazi: (id: number, razlog?: string) => d(api.post<T.Transakcija>(`/transakcije/${id}/otkazi`, { razlog })),
  recenzija: (id: number, ocena: number, komentar?: string) => d(api.post<T.Recenzija>(`/transakcije/${id}/recenzija`, { ocena, komentar }))
}

export const zahtevi = {
  pretraga: (p: { upit?: string; kategorijaId?: number; gradId?: number; stranica?: number }) =>
    d(api.get<T.Stranica<T.Zahtev>>(`/zahtevi?${serijalizuj(p)}`)),
  moji: () => d(api.get<T.Stranica<T.Zahtev>>('/zahtevi/moji')),
  detalji: (id: number) => d(api.get<T.ZahtevDetalji>(`/zahtevi/${id}`)),
  kreiraj: (p: { naslov: string; opis: string; kategorijaId: number; gradId?: number; maksimalnaCena?: number }) =>
    d(api.post<T.Zahtev>('/zahtevi', p)),
  zatvori: (id: number, ispunjen: boolean) => d(api.post<T.Zahtev>(`/zahtevi/${id}/zatvori`, { ispunjen })),
  odgovori: (id: number, oglasId: number, poruka?: string) => api.post(`/zahtevi/${id}/odgovori`, { oglasId, poruka })
}

export const obavestenja = {
  lista: (samoNeprocitana = false, stranica = 1) =>
    d(api.get<T.Stranica<T.Obavestenje>>(`/obavestenja?samoNeprocitana=${samoNeprocitana}&stranica=${stranica}`)),
  brojaci: () => d(api.get<T.Brojaci>('/obavestenja/brojaci')),
  procitano: (id: number) => api.post(`/obavestenja/${id}/procitano`),
  svaProcitana: () => api.post('/obavestenja/procitano')
}

export const moderacija = {
  prijave: (status: T.StatusPrijave | '' = 'Otvorena', stranica = 1) =>
    d(api.get<T.Stranica<T.Prijava>>(`/moderacija/prijave?${serijalizuj({ status: status || undefined, stranica })}`)),
  obradi: (id: number, prihvati: boolean, ukloniOglas: boolean, napomena?: string) =>
    api.post(`/moderacija/prijave/${id}/obrada`, { prihvati, ukloniOglas, napomena }),
  ukloniOglas: (oglasId: number, razlog: string) => api.post(`/moderacija/oglasi/${oglasId}/uklanjanje`, { razlog }),
  korisnici: (upit?: string, stranica = 1) => d(api.get<T.Stranica<T.KorisnikAdmin>>(`/moderacija/korisnici?${serijalizuj({ upit, stranica })}`)),
  blokiraj: (korisnikId: number, razlog: string, trajanjeDana?: number) =>
    api.post(`/moderacija/korisnici/${korisnikId}/blokada`, { razlog, trajanjeDana }),
  blokade: (samoAktivne = true) => d(api.get<T.Stranica<T.BlokadaKorisnika>>(`/moderacija/blokade?samoAktivne=${samoAktivne}`)),
  ukiniBlokadu: (id: number) => api.delete(`/moderacija/blokade/${id}`)
}

export const admin = {
  statistika: () => d(api.get<T.Statistika>('/admin/statistika')),
  audit: (p: { kategorija?: T.KategorijaAudita; stranica?: number }) => d(api.get<T.Stranica<T.AuditZapis>>(`/admin/audit?${serijalizuj(p)}`)),
  uloge: (korisnikId: number, uloge: T.Uloga[]) => api.put(`/admin/korisnici/${korisnikId}/uloge`, { uloge }),
  istaknuti: () => d(api.get<T.Stranica<T.IstaknutiOglas>>('/admin/istaknuti')),
  izdvoji: (oglasId: number, brojDana: number) => api.post(`/admin/oglasi/${oglasId}/izdvajanje`, { brojDana }),
  otkaziIsticanje: (id: number) => api.delete(`/admin/istaknuti/${id}`),
  uredjaji: (minRizik = 30) => d(api.get<T.Stranica<T.OtisakUredjaja>>(`/admin/uredjaji?minRizik=${minRizik}`)),
  blokadeUredjaja: () => d(api.get<T.Stranica<T.BlokadaUredjaja>>('/admin/uredjaji/blokade')),
  blokirajUredjaj: (otisakId: number, razlog: string, trajanjeDana?: number) =>
    api.post(`/admin/uredjaji/${otisakId}/blokada`, { razlog, trajanjeDana }),
  ukiniBlokaduUredjaja: (id: number) => api.delete(`/admin/uredjaji/blokade/${id}`)
}
