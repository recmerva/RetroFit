import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { BadgeCheck, KeyRound, LogOut, Plus, Smartphone, Trash2 } from 'lucide-react'
import { porukaGreske } from '../api/klijent'
import { korisnici, oglasi, sacuvanePretrage } from '../api/servisi'
import type { StatusOglasa } from '../api/tipovi'
import { useAuth } from '../kontekst/AuthKontekst'
import { useToast } from '../kontekst/ToastKontekst'
import { MrezaOglasa } from '../komponente/KarticaOglasa'
import { Greska, Kartice, Naslov, Polje, Prazno, Stranicenje, Ucitavanje, Znacka, Zvezdice } from '../komponente/Ui'
import { cena, datum, nazivBedza, nazivStatusa, nazivStanja } from '../pomocno/format'
import { useGradovi, useUpit } from '../pomocno/kuke'

type Kartica = 'oglasi' | 'podaci' | 'telefon' | 'bezbednost' | 'pretrage'

function MojiOglasi() {
  const [status, setStatus] = useState<StatusOglasa | ''>('')
  const [stranica, setStranica] = useState(1)
  const r = useUpit(() => oglasi.moji(status || undefined, stranica), [status, stranica])

  return (
    <div>
      <div className="mb-4 flex flex-wrap gap-2">
        {(['', 'Aktivan', 'Nacrt', 'Rezervisan', 'Prodat', 'Istekao', 'Uklonjen'] as (StatusOglasa | '')[]).map((s) => (
          <button key={s} onClick={() => { setStatus(s); setStranica(1) }}
            className={`rounded-full px-3 py-1 text-xs font-semibold ${status === s ? 'bg-noc-800 text-white' : 'bg-slate-200 text-slate-700'}`}>
            {s ? nazivStatusa[s] : 'Svi'}
          </button>
        ))}
      </div>
      {r.ucitava ? <Ucitavanje /> : r.greska ? <Greska poruka={r.greska} ponovo={r.ponovo} /> : r.podaci?.stavke.length ? (
        <>
          <MrezaOglasa oglasi={r.podaci.stavke} prikaziStatus />
          <Stranicenje stranica={r.podaci.stranica} ukupnoStranica={r.podaci.ukupnoStranica} promeni={setStranica} />
        </>
      ) : <Prazno naslov="Nema oglasa" akcija={<Link to="/oglasi/novi" className="dugme-primarno"><Plus className="h-4 w-4" /> Postavi oglas</Link>} />}
    </div>
  )
}

function Podaci() {
  const { korisnik, postaviProfil } = useAuth()
  const toast = useToast()
  const gradovi = useGradovi()
  const [p, setP] = useState({
    ime: korisnik!.ime, prezime: korisnik!.prezime, oKorisniku: korisnik!.oKorisniku ?? '', gradId: korisnik!.gradId?.toString() ?? '',
    prikaziTelefonKupcu: korisnik!.prikaziTelefonKupcu
  })
  const [salje, setSalje] = useState(false)

  const sacuvaj = async (e: FormEvent) => {
    e.preventDefault()
    setSalje(true)
    try {
      postaviProfil(await korisnici.azuriraj({ ...p, oKorisniku: p.oKorisniku || undefined, gradId: p.gradId ? Number(p.gradId) : undefined }))
      toast.uspeh('Profil je sačuvan.')
    } catch (err) {
      toast.greska(porukaGreske(err))
    } finally {
      setSalje(false)
    }
  }

  return (
    <form onSubmit={sacuvaj} className="kartica max-w-xl space-y-4 p-6">
      <div className="grid gap-4 sm:grid-cols-2">
        <Polje oznaka="Ime"><input className="unos" value={p.ime} onChange={(e) => setP({ ...p, ime: e.target.value })} /></Polje>
        <Polje oznaka="Prezime"><input className="unos" value={p.prezime} onChange={(e) => setP({ ...p, prezime: e.target.value })} /></Polje>
      </div>
      <Polje oznaka="Grad">
        <select className="unos" value={p.gradId} onChange={(e) => setP({ ...p, gradId: e.target.value })}>
          <option value="">--</option>
          {gradovi.map((g) => <option key={g.id} value={g.id}>{g.naziv}</option>)}
        </select>
      </Polje>
      <Polje oznaka="O meni"><textarea className="unos" rows={3} maxLength={500} value={p.oKorisniku} onChange={(e) => setP({ ...p, oKorisniku: e.target.value })} /></Polje>
      <label className="flex items-start gap-2 text-sm">
        <input type="checkbox" className="mt-1 accent-retro-600" checked={p.prikaziTelefonKupcu} onChange={(e) => setP({ ...p, prikaziTelefonKupcu: e.target.checked })} />
        <span>Prikaži moj (verifikovan) broj telefona kupcu <strong>tek kada prihvatim njegovu ponudu</strong>. Telefon nikada nije javno vidljiv.</span>
      </label>
      <button className="dugme-primarno" disabled={salje}>Sačuvaj</button>
    </form>
  )
}

function Telefon() {
  const { korisnik, postaviProfil } = useAuth()
  const toast = useToast()
  const [telefon, setTelefon] = useState(korisnik!.telefon ?? '')
  const [kod, setKod] = useState('')
  const [poslato, setPoslato] = useState(false)
  const [mockSms, setMockSms] = useState<string | null>(null)

  const posaljiKod = async () => {
    try {
      const r = await korisnici.zatraziVerifikaciju(telefon)
      setPoslato(true)
      toast.uspeh(`Kod je poslat na ${r.telefon}.`)
      // Razvojni režim: mock SMS servis ne šalje pravi SMS - poruku čitamo sa razvojnog endpointa.
      korisnici.razvojSms().then((l) => setMockSms(l.find((s) => s.brojTelefona === r.telefon)?.poruka ?? null)).catch(() => undefined)
    } catch (e) {
      toast.greska(porukaGreske(e))
    }
  }

  const potvrdi = async () => {
    try {
      postaviProfil(await korisnici.potvrdiTelefon(kod))
      toast.uspeh('Broj telefona je verifikovan!')
      setPoslato(false)
      setKod('')
    } catch (e) {
      toast.greska(porukaGreske(e))
    }
  }

  return (
    <div className="kartica max-w-xl space-y-4 p-6">
      <p className="flex items-center gap-2 text-sm">
        {korisnik!.telefonVerifikovan
          ? <><BadgeCheck className="h-5 w-5 text-blue-600" /> Broj <strong>{korisnik!.telefon}</strong> je verifikovan.</>
          : <><Smartphone className="h-5 w-5 text-slate-500" /> Verifikovan telefon donosi bedž i veće poverenje kupaca.</>}
      </p>
      <Polje oznaka="Broj mobilnog telefona" pomoc="Format: 064 123 4567 ili +381 64 123 4567">
        <div className="flex gap-2">
          <input className="unos" value={telefon} onChange={(e) => setTelefon(e.target.value)} />
          <button className="dugme-tamno shrink-0" onClick={posaljiKod} type="button">Pošalji kod</button>
        </div>
      </Polje>
      {poslato && (
        <div className="space-y-2">
          {mockSms && <p className="rounded-lg bg-amber-50 p-3 text-xs text-amber-800"><strong>Mock SMS (razvoj):</strong> {mockSms}</p>}
          <Polje oznaka="Šestocifreni kod iz SMS-a">
            <div className="flex gap-2">
              <input className="unos tracking-widest" inputMode="numeric" maxLength={6} value={kod} onChange={(e) => setKod(e.target.value.replace(/\D/g, ''))} />
              <button className="dugme-primarno shrink-0" onClick={potvrdi} disabled={kod.length !== 6} type="button">Potvrdi</button>
            </div>
          </Polje>
        </div>
      )}
    </div>
  )
}

function Bezbednost() {
  const { odjavi } = useAuth()
  const toast = useToast()
  const navigacija = useNavigate()
  const [stara, setStara] = useState('')
  const [nova, setNova] = useState('')
  const [lozinkaBrisanja, setLozinkaBrisanja] = useState('')

  const promeni = async (e: FormEvent) => {
    e.preventDefault()
    if (nova.length < 8 || !/\d/.test(nova)) {
      toast.greska('Nova lozinka mora imati najmanje 8 karaktera i bar jednu cifru.')
      return
    }
    try {
      await korisnici.promeniLozinku(stara, nova)
      toast.uspeh('Lozinka je promenjena. Odjavljeni ste sa ostalih uređaja.')
      setStara('')
      setNova('')
    } catch (err) {
      toast.greska(porukaGreske(err))
    }
  }

  const obrisi = async () => {
    if (!confirm('Da li ste sigurni? Lični podaci će biti trajno anonimizovani.')) return
    try {
      await korisnici.obrisiNalog(lozinkaBrisanja)
      await odjavi()
      navigacija('/')
    } catch (err) {
      toast.greska(porukaGreske(err))
    }
  }

  return (
    <div className="grid max-w-3xl gap-6 md:grid-cols-2">
      <form onSubmit={promeni} className="kartica space-y-3 p-6">
        <h2 className="flex items-center gap-2 font-semibold"><KeyRound className="h-4 w-4" /> Promena lozinke</h2>
        <Polje oznaka="Trenutna lozinka"><input className="unos" type="password" value={stara} onChange={(e) => setStara(e.target.value)} autoComplete="current-password" /></Polje>
        <Polje oznaka="Nova lozinka"><input className="unos" type="password" value={nova} onChange={(e) => setNova(e.target.value)} autoComplete="new-password" /></Polje>
        <button className="dugme-primarno">Promeni lozinku</button>
      </form>
      <div className="space-y-6">
        <div className="kartica space-y-3 p-6">
          <h2 className="flex items-center gap-2 font-semibold"><LogOut className="h-4 w-4" /> Sesije</h2>
          <p className="text-sm text-slate-600">Odjavite se sa svih uređaja na kojima ste prijavljeni.</p>
          <button className="dugme-sekundarno" onClick={async () => { await odjavi(true); navigacija('/prijava') }}>Odjavi me svuda</button>
        </div>
        <div className="kartica space-y-3 border-red-200 p-6">
          <h2 className="flex items-center gap-2 font-semibold text-red-700"><Trash2 className="h-4 w-4" /> Brisanje naloga</h2>
          <input className="unos" type="password" placeholder="Potvrdite lozinkom" value={lozinkaBrisanja} onChange={(e) => setLozinkaBrisanja(e.target.value)} />
          <button className="dugme-opasno" disabled={!lozinkaBrisanja} onClick={obrisi}>Obriši nalog</button>
        </div>
      </div>
    </div>
  )
}

function Pretrage() {
  const toast = useToast()
  const r = useUpit(() => sacuvanePretrage.lista(), [])
  const obrisi = async (id: number) => {
    try {
      await sacuvanePretrage.obrisi(id)
      r.setPodaci((l) => l?.filter((x) => x.id !== id) ?? l)
    } catch (e) {
      toast.greska(porukaGreske(e))
    }
  }
  if (r.ucitava) return <Ucitavanje />
  if (r.greska) return <Greska poruka={r.greska} />
  if (!r.podaci?.length) return <Prazno naslov="Nema sačuvanih pretraga" opis="Na stranici Oglasi podesite filtere i kliknite 'Sačuvaj pretragu'." />
  return (
    <div className="space-y-2">
      {r.podaci.map((p) => {
        const upit = new URLSearchParams(Object.entries({ upit: p.upit, kategorijaId: p.kategorijaId, gradId: p.gradId, cenaOd: p.cenaOd, cenaDo: p.cenaDo, stanja: p.stanje, tip: p.tip })
          .filter(([, v]) => v !== undefined && v !== null).map(([k, v]) => [k, String(v)]))
        return (
          <div key={p.id} className="kartica flex flex-wrap items-center justify-between gap-2 p-4">
            <div>
              <Link to={`/oglasi?${upit}`} className="font-semibold hover:underline">{p.naziv}</Link>
              <p className="text-xs text-slate-500">
                {[p.upit && `"${p.upit}"`, p.kategorijaNaziv, p.gradNaziv, p.cenaOd && `od ${cena(p.cenaOd)}`, p.cenaDo && `do ${cena(p.cenaDo)}`, p.stanje && nazivStanja[p.stanje], p.tip]
                  .filter(Boolean).join(' · ')}
              </p>
            </div>
            <div className="flex items-center gap-2">
              {p.obavestavaj && <Znacka boja="bg-green-100 text-green-800">Obaveštenja uključena</Znacka>}
              <button className="dugme-sekundarno px-2" onClick={() => obrisi(p.id)} aria-label="Obriši"><Trash2 className="h-4 w-4" /></button>
            </div>
          </div>
        )
      })}
    </div>
  )
}

export default function Profil() {
  const { korisnik } = useAuth()
  const [kartica, setKartica] = useState<Kartica>('oglasi')
  const k = korisnik!

  return (
    <div>
      <div className="kartica mb-6 flex flex-wrap items-center gap-4 p-6">
        <span className="flex h-16 w-16 items-center justify-center rounded-full bg-noc-800 text-2xl font-bold text-white">{k.ime[0]}{k.prezime[0]}</span>
        <div className="flex-1">
          <h1 className="text-2xl font-bold">{k.ime} {k.prezime}</h1>
          <p className="text-sm text-slate-500">{k.email} · član od {datum(k.kreiranoUtc)}</p>
          <div className="mt-1 flex flex-wrap items-center gap-2">
            {k.brojRecenzija > 0 && <Zvezdice ocena={k.prosecnaOcena} broj={k.brojRecenzija} />}
            <span className="text-sm text-slate-600">Reputacija <strong>{k.reputacija}</strong>/100 · {k.brojZavrsenihTransakcija} završenih kupoprodaja</span>
            {k.bedzevi.map((b) => <Znacka key={b} boja="bg-blue-50 text-blue-800">{nazivBedza[b] ?? b}</Znacka>)}
          </div>
        </div>
        <Link to={`/korisnici/${k.id}`} className="dugme-sekundarno">Javni profil</Link>
      </div>
      <Naslov>Moj nalog</Naslov>
      <Kartice<Kartica> aktivna={kartica} promeni={setKartica} kartice={[
        { kljuc: 'oglasi', naziv: 'Moji oglasi' }, { kljuc: 'podaci', naziv: 'Podaci' }, { kljuc: 'telefon', naziv: 'Telefon' },
        { kljuc: 'pretrage', naziv: 'Sačuvane pretrage' }, { kljuc: 'bezbednost', naziv: 'Bezbednost' }
      ]} />
      {kartica === 'oglasi' && <MojiOglasi />}
      {kartica === 'podaci' && <Podaci />}
      {kartica === 'telefon' && <Telefon />}
      {kartica === 'pretrage' && <Pretrage />}
      {kartica === 'bezbednost' && <Bezbednost />}
    </div>
  )
}
