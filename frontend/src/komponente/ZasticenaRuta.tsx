import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router'
import type { Uloga } from '../api/tipovi'
import { useAuth } from '../kontekst/AuthKontekst'
import { Ucitavanje } from './Ui'

/**
 * Zaštićena ruta: neprijavljen korisnik ide na prijavu (pa se posle vraća), a korisnik bez
 * potrebne uloge dobija poruku. NAPOMENA: ovo je samo UX - prava zaštita je na backend-u (JWT + politike).
 */
export function ZasticenaRuta({ children, uloge }: { children: ReactNode; uloge?: Uloga[] }) {
  const { korisnik, ucitava } = useAuth()
  const lokacija = useLocation()

  if (ucitava) return <Ucitavanje tekst="Provera sesije..." />
  if (!korisnik) return <Navigate to="/prijava" replace state={{ nazad: lokacija.pathname + lokacija.search }} />
  if (uloge && !uloge.some((u) => korisnik.uloge.includes(u))) {
    return (
      <div className="kartica mx-auto max-w-md p-8 text-center">
        <h1 className="text-xl font-bold">Nemate pristup</h1>
        <p className="mt-2 text-slate-600">Ova stranica je dostupna samo za: {uloge.join(', ')}.</p>
      </div>
    )
  }
  return <>{children}</>
}
