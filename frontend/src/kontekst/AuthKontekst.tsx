import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { osveziSesiju, postaviAccessToken, postaviSlusaoce } from '../api/klijent'
import { autentifikacija, korisnici } from '../api/servisi'
import type { AutentifikacijaOdgovor, MojProfil, Uloga } from '../api/tipovi'

interface AuthStanje {
  korisnik: MojProfil | null
  ucitava: boolean
  prijavi: (email: string, lozinka: string) => Promise<void>
  registruj: (p: { email: string; lozinka: string; ime: string; prezime: string; gradId?: number }) => Promise<void>
  odjavi: (svuda?: boolean) => Promise<void>
  osveziProfil: () => Promise<void>
  postaviProfil: (p: MojProfil) => void
  imaUlogu: (u: Uloga) => boolean
}

const AuthKontekst = createContext<AuthStanje | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [korisnik, setKorisnik] = useState<MojProfil | null>(null)
  const [ucitava, setUcitava] = useState(true)

  const primeni = useCallback((o: AutentifikacijaOdgovor) => {
    postaviAccessToken(o.accessToken)
    setKorisnik(o.korisnik)
  }, [])

  useEffect(() => {
    postaviSlusaoce(
      () => {
        postaviAccessToken(null)
        setKorisnik(null)
      },
      (o) => setKorisnik(o.korisnik)
    )
    // Pri pokretanju: pokušaj da vratiš sesiju iz httpOnly refresh cookie-ja.
    osveziSesiju().finally(() => setUcitava(false))
  }, [])

  const vrednost = useMemo<AuthStanje>(
    () => ({
      korisnik,
      ucitava,
      prijavi: async (email, lozinka) => primeni(await autentifikacija.prijava(email, lozinka)),
      registruj: async (p) => primeni(await autentifikacija.registracija(p)),
      odjavi: async (svuda = false) => {
        try {
          await (svuda ? autentifikacija.odjavaSvuda() : autentifikacija.odjava())
        } finally {
          postaviAccessToken(null)
          setKorisnik(null)
        }
      },
      osveziProfil: async () => setKorisnik(await korisnici.ja()),
      postaviProfil: setKorisnik,
      imaUlogu: (u) => korisnik?.uloge.includes(u) ?? false
    }),
    [korisnik, ucitava, primeni]
  )

  return <AuthKontekst.Provider value={vrednost}>{children}</AuthKontekst.Provider>
}

export function useAuth() {
  const k = useContext(AuthKontekst)
  if (!k) throw new Error('useAuth mora biti unutar AuthProvider-a')
  return k
}
