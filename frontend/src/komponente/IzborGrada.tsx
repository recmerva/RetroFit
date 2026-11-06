import { useEffect, useId, useMemo, useRef, useState, type KeyboardEvent } from 'react'
import { ChevronDown, MapPin, X } from 'lucide-react'
import type { Grad } from '../api/tipovi'
import { useGradovi } from '../pomocno/kuke'

/** Za pretragu bez kvačica: "cacak" pronalazi "Čačak", "dj" pronalazi "Đ". */
function normalizuj(tekst: string) {
  return tekst.toLowerCase().replace(/đ/g, 'dj').normalize('NFD').replace(/[̀-ͯ]/g, '').trim()
}

interface Props {
  /** Id izabranog grada; prazan string ili 0 znači "ništa nije izabrano". */
  vrednost: string | number | undefined
  promeni: (id: string) => void
  /** Tekst za opciju bez izbora, npr. "Svi gradovi". Ako se ne prosledi, ta opcija ne postoji. */
  praznaOpcija?: string
  placeholder?: string
  oznaka?: string
  className?: string
}

/**
 * Polje za izbor grada sa pretragom (combobox): korisnik kuca deo naziva i bira sa liste.
 * Zamenjuje običan <select>, koji je sa 150+ mesta nepregledan, naročito na telefonu.
 */
export function IzborGrada({ vrednost, promeni, praznaOpcija, placeholder = 'Upišite grad...', oznaka = 'Grad', className = '' }: Props) {
  const gradovi = useGradovi()
  const idListe = useId()
  const koren = useRef<HTMLDivElement>(null)
  const lista = useRef<HTMLUListElement>(null)
  const [otvoren, setOtvoren] = useState(false)
  const [tekst, setTekst] = useState('')
  const [aktivan, setAktivan] = useState(0)

  const izabran = gradovi.find((g) => String(g.id) === String(vrednost ?? ''))

  // Kada lista nije otvorena, polje prikazuje naziv izabranog grada.
  useEffect(() => {
    if (!otvoren) setTekst(izabran?.naziv ?? '')
  }, [izabran, otvoren])

  const ponudjeni = useMemo(() => {
    const upit = normalizuj(tekst)
    if (!upit || upit === normalizuj(izabran?.naziv ?? '')) return gradovi
    const pocinje: Grad[] = []
    const sadrzi: Grad[] = []
    for (const g of gradovi) {
      const n = normalizuj(g.naziv)
      if (n.startsWith(upit) || n.split(' ').some((rec) => rec.startsWith(upit))) pocinje.push(g)
      else if (n.includes(upit)) sadrzi.push(g)
    }
    return [...pocinje, ...sadrzi]
  }, [tekst, gradovi, izabran])

  // Stavke liste: opciona "prazna" opcija (null) + gradovi.
  const stavke: (Grad | null)[] = useMemo(() => (praznaOpcija ? [null, ...ponudjeni] : ponudjeni), [praznaOpcija, ponudjeni])

  // Klik van polja zatvara listu.
  useEffect(() => {
    if (!otvoren) return
    const klik = (e: MouseEvent | TouchEvent) => koren.current && !koren.current.contains(e.target as Node) && setOtvoren(false)
    document.addEventListener('mousedown', klik)
    document.addEventListener('touchstart', klik)
    return () => {
      document.removeEventListener('mousedown', klik)
      document.removeEventListener('touchstart', klik)
    }
  }, [otvoren])

  // Aktivna stavka (strelice gore/dole) uvek ostaje vidljiva.
  useEffect(() => {
    lista.current?.children[aktivan]?.scrollIntoView({ block: 'nearest' })
  }, [aktivan, otvoren])

  const otvori = () => {
    setOtvoren(true)
    const i = stavke.findIndex((s) => (s ? s.id === izabran?.id : !izabran))
    setAktivan(Math.max(i, 0))
  }

  const izaberi = (g: Grad | null) => {
    promeni(g ? String(g.id) : '')
    setTekst(g?.naziv ?? '')
    setOtvoren(false)
  }

  const tastatura = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
      e.preventDefault()
      if (!otvoren) return otvori()
      const pomeraj = e.key === 'ArrowDown' ? 1 : -1
      setAktivan((a) => Math.min(Math.max(a + pomeraj, 0), stavke.length - 1))
    } else if (e.key === 'Enter' && otvoren) {
      e.preventDefault()
      if (stavke[aktivan] !== undefined) izaberi(stavke[aktivan])
    } else if (e.key === 'Escape') {
      setOtvoren(false)
    } else if (e.key === 'Tab') {
      setOtvoren(false)
    }
  }

  return (
    <div ref={koren} className={`relative ${className}`}>
      <MapPin className="pointer-events-none absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2 text-slate-400" />
      <input
        className="unos pr-14 pl-9 text-slate-800"
        role="combobox"
        aria-label={oznaka}
        aria-expanded={otvoren}
        aria-controls={idListe}
        aria-autocomplete="list"
        aria-activedescendant={otvoren && stavke.length ? `${idListe}-${aktivan}` : undefined}
        autoComplete="off"
        placeholder={izabran ? undefined : praznaOpcija ?? placeholder}
        value={tekst}
        onFocus={(e) => {
          otvori()
          e.target.select()
        }}
        onClick={() => !otvoren && otvori()}
        onChange={(e) => {
          setTekst(e.target.value)
          setOtvoren(true)
          setAktivan(praznaOpcija && !e.target.value ? 0 : praznaOpcija ? 1 : 0)
        }}
        onKeyDown={tastatura}
      />
      <span className="absolute top-1/2 right-2 flex -translate-y-1/2 items-center gap-0.5">
        {izabran && praznaOpcija !== undefined && (
          <button type="button" className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700" onClick={() => izaberi(null)} aria-label="Poništi izbor grada">
            <X className="h-4 w-4" />
          </button>
        )}
        <ChevronDown className={`pointer-events-none h-4 w-4 text-slate-400 transition ${otvoren ? 'rotate-180' : ''}`} />
      </span>

      {otvoren && (
        <ul
          ref={lista}
          id={idListe}
          role="listbox"
          aria-label={oznaka}
          className="kartica absolute z-30 mt-1 max-h-64 w-full overflow-y-auto py-1 text-sm text-slate-800 shadow-lg"
        >
          {stavke.map((g, i) => (
            <li
              key={g?.id ?? 'prazno'}
              id={`${idListe}-${i}`}
              role="option"
              aria-selected={g ? g.id === izabran?.id : !izabran}
              onMouseDown={(e) => e.preventDefault()}
              onClick={(e) => {
                // Polje je često unutar <label>; bez ovoga bi label "prosledio" klik polju i ponovo otvorio listu.
                e.preventDefault()
                izaberi(g)
              }}
              onMouseEnter={() => setAktivan(i)}
              className={`flex cursor-pointer items-center justify-between gap-2 px-3 py-2.5 sm:py-2 ${i === aktivan ? 'bg-retro-50 text-retro-800' : ''} ${
                (g ? g.id === izabran?.id : !izabran) ? 'font-semibold' : ''
              }`}
            >
              <span className={g ? '' : 'text-slate-500'}>{g?.naziv ?? praznaOpcija}</span>
              {g && <span className="text-xs text-slate-400">{g.postanskiBroj}</span>}
            </li>
          ))}
          {!ponudjeni.length && <li className="px-3 py-2.5 text-slate-500">Nema mesta sa tim nazivom.</li>}
        </ul>
      )}
    </div>
  )
}
