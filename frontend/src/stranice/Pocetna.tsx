import { useCallback, useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import {
  Baby, Bike, BookOpen, Car, Gamepad2, Home, Laptop, Package, PawPrint, Search, Shirt, Sparkles, Trophy, Wrench
} from 'lucide-react'
import { oglasi } from '../api/servisi'
import type { OglasKartica, StanjeProizvoda, TipOglasa } from '../api/tipovi'
import { MrezaOglasa } from '../komponente/KarticaOglasa'
import { Greska, Prazno, Ucitavanje } from '../komponente/Ui'
import { nazivStanja } from '../pomocno/format'
import { IzborGrada } from '../komponente/IzborGrada'
import { useKategorije, useUpit } from '../pomocno/kuke'
import { usePrebaciOmiljeni } from '../pomocno/omiljeni'

const ikonice: Record<string, typeof Laptop> = {
  elektronika: Laptop, vozila: Car, odeca: Shirt, kuca: Home, sport: Trophy, knjige: BookOpen,
  retro: Gamepad2, deca: Baby, ljubimci: PawPrint, ostalo: Package, bicikl: Bike, alat: Wrench
}

export default function Pocetna() {
  const navigacija = useNavigate()
  const kategorije = useKategorije()
  const [filter, setFilter] = useState({ upit: '', kategorijaId: '', gradId: '', cenaDo: '', stanje: '', tip: '' })

  const izdvojeni = useUpit(() => oglasi.izdvojeni(8), [])
  const najnoviji = useUpit(() => oglasi.pretraga({ sortiranje: 'Najnovije', velicina: 12 }), [])

  const azurirajIzdvojene = useCallback((fn: (o: OglasKartica) => OglasKartica) => izdvojeni.setPodaci((l) => l?.map(fn) ?? l), [izdvojeni])
  const azurirajNajnovije = useCallback(
    (fn: (o: OglasKartica) => OglasKartica) => najnoviji.setPodaci((s) => (s ? { ...s, stavke: s.stavke.map(fn) } : s)),
    [najnoviji]
  )
  const omiljeniIzdvojeni = usePrebaciOmiljeni(azurirajIzdvojene)
  const omiljeniNajnoviji = usePrebaciOmiljeni(azurirajNajnovije)

  const pretrazi = (e: FormEvent) => {
    e.preventDefault()
    const p = new URLSearchParams()
    Object.entries(filter).forEach(([k, v]) => {
      if (!v) return
      p.set(k === 'stanje' ? 'stanja' : k, v)
    })
    navigacija(`/oglasi?${p.toString()}`)
  }

  return (
    <div className="space-y-10">
      <section className="rounded-2xl bg-gradient-to-br from-noc-800 via-noc-700 to-retro-700 p-5 text-white shadow-lg sm:p-8 lg:p-12">
        <h1 className="max-w-2xl text-2xl font-black text-white sm:text-3xl lg:text-4xl">Polovne stvari, nova priča.</h1>
        <p className="mt-2 max-w-xl text-sm text-slate-200 sm:text-base">Kupi, prodaj, razmeni ili pokloni - lokalno, sigurno i bez komplikacija.</p>
        {/* Telefon: jedna kolona; tablet: dve; desktop: sve u jednom redu. */}
        <form onSubmit={pretrazi} className="mt-6 grid gap-2 rounded-xl bg-white/10 p-3 backdrop-blur sm:grid-cols-2 lg:grid-cols-6" role="search">
          <div className="relative sm:col-span-2">
            <Search className="absolute top-2.5 left-3 h-4 w-4 text-slate-400" />
            <input className="unos pl-9 text-slate-800" placeholder="Šta tražite?" value={filter.upit}
              onChange={(e) => setFilter({ ...filter, upit: e.target.value })} aria-label="Pojam za pretragu" />
          </div>
          <select className="unos text-slate-800" value={filter.kategorijaId} onChange={(e) => setFilter({ ...filter, kategorijaId: e.target.value })}
            aria-label="Kategorija">
            <option value="">Sve kategorije</option>
            {kategorije.map((k) => <option key={k.id} value={k.id}>{k.naziv}</option>)}
          </select>
          <IzborGrada vrednost={filter.gradId} promeni={(gradId) => setFilter({ ...filter, gradId })} praznaOpcija="Svi gradovi" />
          <select className="unos text-slate-800" value={filter.stanje} onChange={(e) => setFilter({ ...filter, stanje: e.target.value })} aria-label="Stanje">
            <option value="">Bilo koje stanje</option>
            {(Object.keys(nazivStanja) as StanjeProizvoda[]).map((s) => <option key={s} value={s}>{nazivStanja[s]}</option>)}
          </select>
          <button className="dugme-primarno">Pretraži</button>
          <div className="flex flex-wrap items-center gap-2 sm:col-span-2 lg:col-span-6">
            {(['', 'Prodaja', 'Razmena', 'Poklon'] as (TipOglasa | '')[]).map((t) => (
              <button type="button" key={t} onClick={() => setFilter({ ...filter, tip: t })}
                className={`rounded-full px-3 py-1 text-xs font-semibold ${filter.tip === t ? 'bg-retro-500 text-white' : 'bg-white/20 text-white hover:bg-white/30'}`}>
                {t || 'Sve'}
              </button>
            ))}
            <input className="unos w-36 py-1 text-xs text-slate-800" type="number" min={0} placeholder="Cena do (RSD)" value={filter.cenaDo}
              onChange={(e) => setFilter({ ...filter, cenaDo: e.target.value })} aria-label="Maksimalna cena" />
          </div>
        </form>
      </section>

      <section>
        <h2 className="mb-4 text-xl font-bold">Kategorije</h2>
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
          {kategorije.map((k) => {
            const Ikonica = ikonice[k.ikonica ?? 'ostalo'] ?? Package
            return (
              <Link key={k.id} to={`/oglasi?kategorijaId=${k.id}`}
                className="kartica flex items-center gap-3 p-3 transition hover:border-retro-300 hover:shadow-md">
                <span className="shrink-0 rounded-lg bg-retro-50 p-2 text-retro-600"><Ikonica className="h-5 w-5" /></span>
                <span className="min-w-0">
                  {/* Ceo naziv u najviše dva reda, umesto odsecanja ("Knjige, muzika i ..."). */}
                  <span className="line-clamp-2 text-sm leading-snug font-semibold break-words">{k.naziv}</span>
                  <span className="text-xs text-slate-500">{k.brojAktivnihOglasa} oglasa</span>
                </span>
              </Link>
            )
          })}
        </div>
      </section>

      <section>
        <h2 className="mb-4 flex items-center gap-2 text-xl font-bold"><Sparkles className="h-5 w-5 text-retro-500" /> Izdvojeni oglasi</h2>
        {izdvojeni.ucitava ? <Ucitavanje /> : izdvojeni.greska ? <Greska poruka={izdvojeni.greska} ponovo={izdvojeni.ponovo} />
          : izdvojeni.podaci?.length ? <MrezaOglasa oglasi={izdvojeni.podaci} promeniOmiljeni={omiljeniIzdvojeni} />
          : <Prazno naslov="Trenutno nema izdvojenih oglasa" />}
      </section>

      <section>
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-xl font-bold">Najnoviji oglasi</h2>
          <Link to="/oglasi" className="text-sm font-semibold text-retro-700 hover:underline">Svi oglasi →</Link>
        </div>
        {najnoviji.ucitava ? <Ucitavanje /> : najnoviji.greska ? <Greska poruka={najnoviji.greska} ponovo={najnoviji.ponovo} />
          : najnoviji.podaci?.stavke.length ? <MrezaOglasa oglasi={najnoviji.podaci.stavke} promeniOmiljeni={omiljeniNajnoviji} />
          : <Prazno naslov="Još nema oglasa" opis="Budite prvi koji će postaviti oglas!" akcija={<Link className="dugme-primarno" to="/oglasi/novi">Postavi oglas</Link>} />}
      </section>
    </div>
  )
}
