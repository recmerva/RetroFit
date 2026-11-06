import { useEffect, type ReactNode } from 'react'
import { AlertTriangle, ChevronLeft, ChevronRight, Inbox, Loader2, Star, X } from 'lucide-react'
import type { StatusOglasa } from '../api/tipovi'
import { bojaStatusa, nazivStatusa } from '../pomocno/format'

export function Ucitavanje({ tekst = 'Učitavanje...' }: { tekst?: string }) {
  return (
    <div className="flex items-center justify-center gap-2 py-16 text-slate-500" role="status">
      <Loader2 className="h-5 w-5 animate-spin" />
      <span>{tekst}</span>
    </div>
  )
}

export function Greska({ poruka, ponovo }: { poruka: string; ponovo?: () => void }) {
  return (
    <div className="kartica flex flex-col items-center gap-3 border-red-200 bg-red-50 p-8 text-center text-red-800" role="alert">
      <AlertTriangle className="h-8 w-8" />
      <p>{poruka}</p>
      {ponovo && (
        <button className="dugme-sekundarno" onClick={ponovo}>
          Pokušaj ponovo
        </button>
      )}
    </div>
  )
}

export function Prazno({ naslov, opis, akcija }: { naslov: string; opis?: string; akcija?: ReactNode }) {
  return (
    <div className="kartica flex flex-col items-center gap-2 p-10 text-center">
      <Inbox className="h-10 w-10 text-slate-300" />
      <p className="font-semibold text-slate-700">{naslov}</p>
      {opis && <p className="max-w-md text-sm text-slate-500">{opis}</p>}
      {akcija && <div className="mt-2">{akcija}</div>}
    </div>
  )
}

export function Stranicenje({ stranica, ukupnoStranica, promeni }: { stranica: number; ukupnoStranica: number; promeni: (s: number) => void }) {
  if (ukupnoStranica <= 1) return null
  const brojevi = Array.from({ length: ukupnoStranica }, (_, i) => i + 1).filter(
    (s) => s === 1 || s === ukupnoStranica || Math.abs(s - stranica) <= 2
  )
  return (
    <nav className="mt-6 flex items-center justify-center gap-1" aria-label="Stranice">
      <button className="dugme-sekundarno px-2" disabled={stranica <= 1} onClick={() => promeni(stranica - 1)} aria-label="Prethodna">
        <ChevronLeft className="h-4 w-4" />
      </button>
      {brojevi.map((s, i) => (
        <span key={s} className="flex items-center">
          {i > 0 && brojevi[i - 1] !== s - 1 && <span className="px-1 text-slate-400">…</span>}
          <button
            className={s === stranica ? 'dugme-primarno px-3' : 'dugme-sekundarno px-3'}
            onClick={() => promeni(s)}
            aria-current={s === stranica ? 'page' : undefined}
          >
            {s}
          </button>
        </span>
      ))}
      <button className="dugme-sekundarno px-2" disabled={stranica >= ukupnoStranica} onClick={() => promeni(stranica + 1)} aria-label="Sledeća">
        <ChevronRight className="h-4 w-4" />
      </button>
    </nav>
  )
}

export function Zvezdice({ ocena, broj, velicina = 'h-4 w-4' }: { ocena: number; broj?: number; velicina?: string }) {
  return (
    <span className="inline-flex items-center gap-0.5" title={`${ocena.toFixed(1)} / 5`}>
      {[1, 2, 3, 4, 5].map((i) => (
        <Star key={i} className={`${velicina} ${i <= Math.round(ocena) ? 'fill-amber-400 text-amber-400' : 'text-slate-300'}`} />
      ))}
      {broj !== undefined && <span className="ml-1 text-xs text-slate-500">({broj})</span>}
    </span>
  )
}

export function BiracOcene({ vrednost, promeni }: { vrednost: number; promeni: (o: number) => void }) {
  return (
    <div className="flex gap-1" role="radiogroup" aria-label="Ocena">
      {[1, 2, 3, 4, 5].map((i) => (
        <button key={i} type="button" onClick={() => promeni(i)} aria-label={`Ocena ${i}`} aria-checked={vrednost === i} role="radio">
          <Star className={`h-7 w-7 ${i <= vrednost ? 'fill-amber-400 text-amber-400' : 'text-slate-300 hover:text-amber-300'}`} />
        </button>
      ))}
    </div>
  )
}

export function StatusZnacka({ status }: { status: StatusOglasa }) {
  return <span className={`znacka ${bojaStatusa[status]}`}>{nazivStatusa[status]}</span>
}

export function Znacka({ children, boja = 'bg-slate-100 text-slate-700' }: { children: ReactNode; boja?: string }) {
  return <span className={`znacka ${boja}`}>{children}</span>
}

export function Modal({ naslov, otvoren, zatvori, children }: { naslov: string; otvoren: boolean; zatvori: () => void; children: ReactNode }) {
  useEffect(() => {
    if (!otvoren) return
    const esc = (e: KeyboardEvent) => e.key === 'Escape' && zatvori()
    window.addEventListener('keydown', esc)
    return () => window.removeEventListener('keydown', esc)
  }, [otvoren, zatvori])

  if (!otvoren) return null
  return (
    <div className="fixed inset-0 z-40 flex items-center justify-center bg-black/40 p-4" onClick={zatvori}>
      <div className="kartica w-full max-w-lg p-6" role="dialog" aria-modal="true" aria-label={naslov} onClick={(e) => e.stopPropagation()}>
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-bold">{naslov}</h2>
          <button onClick={zatvori} aria-label="Zatvori">
            <X className="h-5 w-5 text-slate-500" />
          </button>
        </div>
        {children}
      </div>
    </div>
  )
}

export function Polje({ oznaka, greska, children, pomoc }: { oznaka: string; greska?: string; children: ReactNode; pomoc?: string }) {
  return (
    <label className="block">
      <span className="oznaka">{oznaka}</span>
      {children}
      {pomoc && !greska && <span className="mt-1 block text-xs text-slate-500">{pomoc}</span>}
      {greska && <span className="mt-1 block text-xs text-red-600">{greska}</span>}
    </label>
  )
}

export function Naslov({ children, akcija }: { children: ReactNode; akcija?: ReactNode }) {
  return (
    <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
      <h1 className="text-2xl font-bold">{children}</h1>
      {akcija}
    </div>
  )
}

export function Kartice<T extends string>({ kartice, aktivna, promeni }: { kartice: { kljuc: T; naziv: string; broj?: number }[]; aktivna: T; promeni: (k: T) => void }) {
  return (
    <div className="mb-4 flex flex-wrap gap-1 border-b border-slate-200" role="tablist">
      {kartice.map((k) => (
        <button
          key={k.kljuc}
          role="tab"
          aria-selected={k.kljuc === aktivna}
          onClick={() => promeni(k.kljuc)}
          className={`-mb-px border-b-2 px-4 py-2 text-sm font-semibold ${k.kljuc === aktivna ? 'border-retro-600 text-retro-700' : 'border-transparent text-slate-500 hover:text-slate-800'}`}
        >
          {k.naziv}
          {k.broj ? <span className="ml-1.5 rounded-full bg-retro-100 px-1.5 text-xs text-retro-700">{k.broj}</span> : null}
        </button>
      ))}
    </div>
  )
}
