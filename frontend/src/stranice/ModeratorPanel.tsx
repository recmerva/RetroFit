import { useState } from 'react'
import { Link } from 'react-router'
import { Ban, Check, ShieldX, X } from 'lucide-react'
import { porukaGreske } from '../api/klijent'
import { moderacija } from '../api/servisi'
import type { KorisnikAdmin, Prijava, StatusPrijave } from '../api/tipovi'
import { useToast } from '../kontekst/ToastKontekst'
import { Greska, Kartice, Modal, Naslov, Prazno, StatusZnacka, Stranicenje, Ucitavanje, Znacka } from '../komponente/Ui'
import { datum, datumVreme, nazivRazloga, relativno } from '../pomocno/format'
import { useUpit } from '../pomocno/kuke'

function Prijave() {
  const toast = useToast()
  const [status, setStatus] = useState<StatusPrijave | ''>('Otvorena')
  const [stranica, setStranica] = useState(1)
  const r = useUpit(() => moderacija.prijave(status, stranica), [status, stranica])
  const [obrada, setObrada] = useState<Prijava | null>(null)
  const [napomena, setNapomena] = useState('')

  const obradi = async (prihvati: boolean, ukloni: boolean) => {
    if (!obrada) return
    try {
      await moderacija.obradi(obrada.id, prihvati, ukloni, napomena.trim() || undefined)
      toast.uspeh(prihvati ? (ukloni ? 'Prijava prihvaćena, oglas uklonjen.' : 'Prijava prihvaćena.') : 'Prijava odbijena.')
      setObrada(null)
      setNapomena('')
      await r.ponovo()
    } catch (e) {
      toast.greska(porukaGreske(e))
    }
  }

  return (
    <div>
      <select className="unos mb-4 w-auto" value={status} onChange={(e) => { setStatus(e.target.value as StatusPrijave | ''); setStranica(1) }}>
        <option value="Otvorena">Otvorene</option>
        <option value="Prihvacena">Prihvaćene</option>
        <option value="Odbijena">Odbijene</option>
        <option value="">Sve</option>
      </select>
      {r.ucitava ? <Ucitavanje /> : r.greska ? <Greska poruka={r.greska} ponovo={r.ponovo} /> : r.podaci?.stavke.length ? (
        <div className="space-y-3">
          {r.podaci.stavke.map((p) => (
            <div key={p.id} className="kartica flex flex-wrap items-center gap-4 p-4">
              <div className="min-w-0 flex-1">
                <div className="flex flex-wrap items-center gap-2">
                  <Link to={`/oglasi/${p.oglasId}`} className="font-semibold hover:underline">{p.oglasNaslov}</Link>
                  <StatusZnacka status={p.oglasStatus} />
                  {p.brojOtvorenihPrijavaZaOglas > 1 && <Znacka boja="bg-red-100 text-red-800">{p.brojOtvorenihPrijavaZaOglas} prijave</Znacka>}
                </div>
                <p className="text-sm"><strong>{nazivRazloga[p.razlog]}</strong>{p.opis && ` - ${p.opis}`}</p>
                <p className="text-xs text-slate-500">
                  Prijavio {p.prijavioIme} {relativno(p.kreiranoUtc)} · prodavac {p.prodavacIme}
                  {p.obradioIme && ` · obradio ${p.obradioIme} ${datumVreme(p.obradjenoUtc)}`}
                </p>
                {p.napomenaModeratora && <p className="text-xs text-slate-600">Napomena: {p.napomenaModeratora}</p>}
              </div>
              {p.status === 'Otvorena' ? <button className="dugme-tamno" onClick={() => setObrada(p)}>Obradi</button> : <Znacka>{p.status}</Znacka>}
            </div>
          ))}
          <Stranicenje stranica={r.podaci.stranica} ukupnoStranica={r.podaci.ukupnoStranica} promeni={setStranica} />
        </div>
      ) : <Prazno naslov="Nema prijava" opis="Sve je čisto!" />}
      <Modal naslov={`Prijava: ${obrada?.oglasNaslov ?? ''}`} otvoren={!!obrada} zatvori={() => setObrada(null)}>
        <textarea className="unos mb-3" rows={3} placeholder="Napomena / razlog uklanjanja (obavezno za uklanjanje)" value={napomena} onChange={(e) => setNapomena(e.target.value)} />
        <div className="grid gap-2">
          <button className="dugme-opasno" disabled={!napomena.trim()} onClick={() => obradi(true, true)}><ShieldX className="h-4 w-4" /> Prihvati i ukloni oglas</button>
          <button className="dugme-primarno" onClick={() => obradi(true, false)}><Check className="h-4 w-4" /> Prihvati (bez uklanjanja)</button>
          <button className="dugme-sekundarno" onClick={() => obradi(false, false)}><X className="h-4 w-4" /> Odbij prijavu</button>
        </div>
      </Modal>
    </div>
  )
}

function Korisnici() {
  const toast = useToast()
  const [upit, setUpit] = useState('')
  const [primenjen, setPrimenjen] = useState('')
  const [stranica, setStranica] = useState(1)
  const r = useUpit(() => moderacija.korisnici(primenjen || undefined, stranica), [primenjen, stranica])
  const [blokada, setBlokada] = useState<KorisnikAdmin | null>(null)
  const [razlog, setRazlog] = useState('')
  const [dana, setDana] = useState('7')

  const blokiraj = async () => {
    if (!blokada) return
    try {
      await moderacija.blokiraj(blokada.id, razlog.trim(), dana ? Number(dana) : undefined)
      toast.uspeh(`${blokada.imePrezime} je blokiran/a.`)
      setBlokada(null)
      setRazlog('')
      await r.ponovo()
    } catch (e) {
      toast.greska(porukaGreske(e))
    }
  }

  return (
    <div>
      <form className="mb-4 flex gap-2" onSubmit={(e) => { e.preventDefault(); setPrimenjen(upit); setStranica(1) }}>
        <input className="unos" placeholder="Email, ime ili prezime" value={upit} onChange={(e) => setUpit(e.target.value)} />
        <button className="dugme-tamno">Traži</button>
      </form>
      {r.ucitava ? <Ucitavanje /> : r.greska ? <Greska poruka={r.greska} /> : r.podaci?.stavke.length ? (
        <div className="kartica overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead className="bg-slate-50 text-xs text-slate-500"><tr><th className="p-3">Korisnik</th><th>Uloge</th><th>Oglasa</th><th>Registrovan</th><th>Poslednja prijava</th><th /></tr></thead>
            <tbody>
              {r.podaci.stavke.map((k) => (
                <tr key={k.id} className="border-t border-slate-100">
                  <td className="p-3"><Link to={`/korisnici/${k.id}`} className="font-medium hover:underline">{k.imePrezime}</Link><span className="block text-xs text-slate-500">{k.email}</span></td>
                  <td>{k.uloge.join(', ')}</td>
                  <td>{k.brojOglasa}</td>
                  <td>{datum(k.kreiranoUtc)}</td>
                  <td>{relativno(k.poslednjaPrijavaUtc) || '-'}</td>
                  <td className="pr-3 text-right">
                    {k.obrisan ? <Znacka>Obrisan</Znacka> : k.blokiran ? <Znacka boja="bg-red-100 text-red-800">Blokiran</Znacka> : !k.uloge.includes('Admin') && (
                      <button className="dugme-sekundarno px-2 py-1 text-red-600" onClick={() => setBlokada(k)}><Ban className="h-4 w-4" /> Blokiraj</button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <Stranicenje stranica={r.podaci.stranica} ukupnoStranica={r.podaci.ukupnoStranica} promeni={setStranica} />
        </div>
      ) : <Prazno naslov="Nema korisnika" />}
      <Modal naslov={`Blokiranje: ${blokada?.imePrezime ?? ''}`} otvoren={!!blokada} zatvori={() => setBlokada(null)}>
        <textarea className="unos mb-3" rows={3} placeholder="Razlog (vidi ga korisnik)" value={razlog} onChange={(e) => setRazlog(e.target.value)} />
        <select className="unos mb-3" value={dana} onChange={(e) => setDana(e.target.value)}>
          <option value="1">1 dan</option><option value="7">7 dana</option><option value="30">30 dana</option><option value="">Trajno</option>
        </select>
        <button className="dugme-opasno w-full" disabled={!razlog.trim()} onClick={blokiraj}>Blokiraj (odjavljuje ga sa svih uređaja)</button>
      </Modal>
    </div>
  )
}

function Blokade() {
  const toast = useToast()
  const [samoAktivne, setSamoAktivne] = useState(true)
  const r = useUpit(() => moderacija.blokade(samoAktivne), [samoAktivne])
  const ukini = async (id: number) => {
    try {
      await moderacija.ukiniBlokadu(id)
      toast.uspeh('Blokada je ukinuta.')
      await r.ponovo()
    } catch (e) {
      toast.greska(porukaGreske(e))
    }
  }
  return (
    <div>
      <label className="mb-4 flex items-center gap-2 text-sm">
        <input type="checkbox" className="accent-retro-600" checked={samoAktivne} onChange={(e) => setSamoAktivne(e.target.checked)} /> Samo aktivne
      </label>
      {r.ucitava ? <Ucitavanje /> : r.podaci?.stavke.length ? (
        <div className="space-y-2">
          {r.podaci.stavke.map((b) => (
            <div key={b.id} className="kartica flex flex-wrap items-center gap-3 p-4">
              <div className="flex-1">
                <p className="font-semibold">{b.korisnikIme} <span className="text-xs font-normal text-slate-500">{b.korisnikEmail}</span></p>
                <p className="text-sm text-slate-600">{b.razlog}</p>
                <p className="text-xs text-slate-500">Blokirao {b.blokiraoIme} {datumVreme(b.pocetakUtc)} · {b.krajUtc ? `do ${datumVreme(b.krajUtc)}` : 'trajno'}{b.ukinutoUtc && ` · ukinuto ${datumVreme(b.ukinutoUtc)}`}</p>
              </div>
              {b.aktivna ? <button className="dugme-sekundarno" onClick={() => ukini(b.id)}>Ukini blokadu</button> : <Znacka>Neaktivna</Znacka>}
            </div>
          ))}
        </div>
      ) : <Prazno naslov="Nema blokada" />}
    </div>
  )
}

export default function ModeratorPanel() {
  const [kartica, setKartica] = useState<'prijave' | 'korisnici' | 'blokade'>('prijave')
  return (
    <div>
      <Naslov>Moderacija</Naslov>
      <Kartice aktivna={kartica} promeni={setKartica} kartice={[
        { kljuc: 'prijave', naziv: 'Prijave oglasa' }, { kljuc: 'korisnici', naziv: 'Korisnici' }, { kljuc: 'blokade', naziv: 'Blokade' }
      ]} />
      {kartica === 'prijave' && <Prijave />}
      {kartica === 'korisnici' && <Korisnici />}
      {kartica === 'blokade' && <Blokade />}
    </div>
  )
}
