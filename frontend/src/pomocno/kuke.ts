import { useCallback, useEffect, useRef, useState } from 'react'
import { porukaGreske } from '../api/klijent'
import { katalog, obavestenja } from '../api/servisi'
import type { Brojaci, Grad, Kategorija } from '../api/tipovi'

/** Učitavanje podataka sa stanjima: ucitava / greska / podaci, i ponovnim učitavanjem. */
export function useUpit<T>(funkcija: () => Promise<T>, zavisnosti: unknown[]) {
  const [podaci, setPodaci] = useState<T | null>(null)
  const [greska, setGreska] = useState<string | null>(null)
  const [ucitava, setUcitava] = useState(true)
  const brojac = useRef(0)

  const ucitaj = useCallback(async () => {
    const moj = ++brojac.current
    setUcitava(true)
    setGreska(null)
    try {
      const r = await funkcija()
      if (moj === brojac.current) setPodaci(r)
    } catch (e) {
      if (moj === brojac.current) setGreska(porukaGreske(e))
    } finally {
      if (moj === brojac.current) setUcitava(false)
    }
  }, zavisnosti)

  useEffect(() => {
    void ucitaj()
  }, [ucitaj])

  return { podaci, greska, ucitava, ponovo: ucitaj, setPodaci }
}

// Šifarnici se učitavaju jednom i keširaju za celu sesiju.
let kesKategorija: Promise<Kategorija[]> | null = null
let kesGradova: Promise<Grad[]> | null = null

export function useKategorije() {
  const [k, setK] = useState<Kategorija[]>([])
  useEffect(() => {
    kesKategorija ??= katalog.kategorije().catch((e) => {
      kesKategorija = null
      throw e
    })
    kesKategorija.then(setK).catch(() => setK([]))
  }, [])
  return k
}

export function osveziKesKategorija() {
  kesKategorija = null
}

export function useGradovi() {
  const [g, setG] = useState<Grad[]>([])
  useEffect(() => {
    kesGradova ??= katalog.gradovi().catch((e) => {
      kesGradova = null
      throw e
    })
    kesGradova.then(setG).catch(() => setG([]))
  }, [])
  return g
}

/** Brojači za navigaciju, osvežavaju se na svakih 30 sekundi dok je korisnik prijavljen. */
export function useBrojaci(aktivno: boolean) {
  const [b, setB] = useState<Brojaci | null>(null)
  useEffect(() => {
    if (!aktivno) {
      setB(null)
      return
    }
    const ucitaj = () => obavestenja.brojaci().then(setB).catch(() => undefined)
    void ucitaj()
    const t = setInterval(ucitaj, 30_000)
    return () => clearInterval(t)
  }, [aktivno])
  return b
}
