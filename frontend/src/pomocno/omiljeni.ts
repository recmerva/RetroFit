import { useCallback } from 'react'
import { useNavigate } from 'react-router'
import { porukaGreske } from '../api/klijent'
import { omiljeni } from '../api/servisi'
import type { OglasKartica } from '../api/tipovi'
import { useAuth } from '../kontekst/AuthKontekst'
import { useToast } from '../kontekst/ToastKontekst'

/**
 * Prebacivanje "omiljenog" sa optimističkim ažuriranjem UI-ja: srce se odmah menja,
 * a ako API vrati grešku, promena se poništava.
 */
export function usePrebaciOmiljeni(azuriraj: (fn: (o: OglasKartica) => OglasKartica) => void) {
  const { korisnik } = useAuth()
  const toast = useToast()
  const navigacija = useNavigate()

  return useCallback(
    async (oglas: OglasKartica) => {
      if (!korisnik) {
        navigacija('/prijava')
        return
      }
      const novo = !oglas.jeOmiljen
      const primeni = (vrednost: boolean) =>
        azuriraj((o) => (o.id === oglas.id ? { ...o, jeOmiljen: vrednost, brojOmiljenih: o.brojOmiljenih + (vrednost ? 1 : -1) } : o))
      primeni(novo)
      try {
        await (novo ? omiljeni.dodaj(oglas.id) : omiljeni.ukloni(oglas.id))
      } catch (e) {
        primeni(!novo)
        toast.greska(porukaGreske(e))
      }
    },
    [korisnik, azuriraj, toast, navigacija]
  )
}
