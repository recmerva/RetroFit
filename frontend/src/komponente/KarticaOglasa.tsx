import { Link } from 'react-router'
import { BadgeCheck, Heart, MapPin, Package, Sparkles } from 'lucide-react'
import type { OglasKartica } from '../api/tipovi'
import { cena, nazivStanja, relativno } from '../pomocno/format'
import { StatusZnacka, Zvezdice } from './Ui'

export function SlikaIliZamena({ putanja, naslov, klasa = 'h-48' }: { putanja?: string; naslov: string; klasa?: string }) {
  if (putanja) {
    return <img src={putanja} alt={naslov} loading="lazy" className={`${klasa} w-full object-cover`} />
  }
  return (
    <div className={`${klasa} flex w-full items-center justify-center bg-gradient-to-br from-retro-100 to-slate-100`} aria-label="Bez slike">
      <Package className="h-12 w-12 text-retro-300" />
    </div>
  )
}

interface Props {
  oglas: OglasKartica
  promeniOmiljeni?: (oglas: OglasKartica) => void
  prikaziStatus?: boolean
}

/** Kartica oglasa: slika, naslov, cena, grad, stanje, datum objave, prodavac i njegova ocena. */
export function KarticaOglasa({ oglas, promeniOmiljeni, prikaziStatus }: Props) {
  return (
    <article className="kartica group relative flex flex-col overflow-hidden transition hover:-translate-y-0.5 hover:shadow-md">
      <Link to={`/oglasi/${oglas.id}`} className="block">
        <div className="relative">
          <SlikaIliZamena putanja={oglas.glavnaSlika} naslov={oglas.naslov} />
          {oglas.jeIstaknut && (
            <span className="znacka absolute top-2 left-2 bg-retro-600 text-white shadow">
              <Sparkles className="h-3 w-3" /> Istaknuto
            </span>
          )}
          {prikaziStatus && (
            <span className="absolute bottom-2 left-2">
              <StatusZnacka status={oglas.status} />
            </span>
          )}
        </div>
        <div className="flex flex-1 flex-col gap-1 p-3">
          <h3 className="line-clamp-2 font-semibold text-slate-900 group-hover:text-retro-700">{oglas.naslov}</h3>
          <p className="text-lg font-bold text-retro-700">{cena(oglas.cena, oglas.tip)}</p>
          <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-slate-500">
            <span className="inline-flex items-center gap-1">
              <MapPin className="h-3 w-3" />
              {oglas.gradNaziv}
              {oglas.udaljenostKm !== undefined && oglas.udaljenostKm !== null && ` · ${Math.round(oglas.udaljenostKm)} km`}
            </span>
            <span>{nazivStanja[oglas.stanje]}</span>
            <span>{relativno(oglas.objavljenoUtc)}</span>
          </div>
          <div className="mt-auto flex items-center justify-between border-t border-slate-100 pt-2 text-xs">
            <span className="inline-flex items-center gap-1 font-medium text-slate-700">
              {oglas.prodavac.prikaznoIme}
              {oglas.prodavac.telefonVerifikovan && <BadgeCheck className="h-3.5 w-3.5 text-blue-600" aria-label="Verifikovan" />}
            </span>
            {oglas.prodavac.brojRecenzija > 0 ? (
              <Zvezdice ocena={oglas.prodavac.prosecnaOcena} broj={oglas.prodavac.brojRecenzija} velicina="h-3 w-3" />
            ) : (
              <span className="text-slate-400">Bez ocena</span>
            )}
          </div>
        </div>
      </Link>
      {promeniOmiljeni && (
        <button
          onClick={() => promeniOmiljeni(oglas)}
          className="absolute top-2 right-2 rounded-full bg-white/90 p-2 shadow hover:bg-white"
          aria-label={oglas.jeOmiljen ? 'Ukloni iz omiljenih' : 'Dodaj u omiljene'}
          aria-pressed={oglas.jeOmiljen}
        >
          <Heart className={`h-4 w-4 ${oglas.jeOmiljen ? 'fill-red-500 text-red-500' : 'text-slate-600'}`} />
        </button>
      )}
    </article>
  )
}

export function MrezaOglasa({ oglasi, promeniOmiljeni, prikaziStatus }: { oglasi: OglasKartica[] } & Omit<Props, 'oglas'>) {
  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
      {oglasi.map((o) => (
        <KarticaOglasa key={o.id} oglas={o} promeniOmiljeni={promeniOmiljeni} prikaziStatus={prikaziStatus} />
      ))}
    </div>
  )
}
