import axios, { AxiosError, type AxiosRequestConfig, type InternalAxiosRequestConfig } from 'axios'
import { klijentskiSignali, resiIzazov, sacuvajTokenIzazova, tokenIzazova, type Izazov } from './bezbednost'
import type { AutentifikacijaOdgovor, ProblemDetails } from './tipovi'

/**
 * Axios klijent za RetroFit API.
 *
 * Bezbednost tokena:
 *  - access token se čuva SAMO u memoriji (promenljiva ispod), ne u localStorage (XSS ga ne može lako ukrasti);
 *  - refresh token je u httpOnly cookie-ju koji postavlja server - JavaScript ga uopšte ne vidi.
 * Posle osvežavanja stranice, sesija se vraća pozivom /autentifikacija/osvezi (browser sam šalje cookie).
 */
export const api = axios.create({ baseURL: '/api/v1', withCredentials: true, timeout: 20_000 })

let accessToken: string | null = null
let naIstekSesije: (() => void) | null = null
let naNovuSesiju: ((o: AutentifikacijaOdgovor) => void) | null = null

export function postaviAccessToken(token: string | null) {
  accessToken = token
}

export function postaviSlusaoce(istekSesije: () => void, novaSesija: (o: AutentifikacijaOdgovor) => void) {
  naIstekSesije = istekSesije
  naNovuSesiju = novaSesija
}

api.interceptors.request.use((config) => {
  if (accessToken) config.headers.Authorization = `Bearer ${accessToken}`
  config.headers['X-Client-Signals'] = klijentskiSignali()
  const izazov = tokenIzazova()
  if (izazov) config.headers['X-Challenge-Token'] = izazov
  return config
})

// Jedno zajedničko osvežavanje za sve pozivaoce (učitavanje stranice, zahtevi koji istovremeno dobiju 401,
// dvostruki efekat u React StrictMode-u). Refresh token je jednokratan: dva paralelna poziva sa istim
// kolačićem server bi video kao ponovnu upotrebu tokena.
let osvezavanje: Promise<AutentifikacijaOdgovor | null> | null = null

export function osveziSesiju(): Promise<AutentifikacijaOdgovor | null> {
  osvezavanje ??= izvrsiOsvezavanje().finally(() => (osvezavanje = null))
  return osvezavanje
}

async function izvrsiOsvezavanje(): Promise<AutentifikacijaOdgovor | null> {
  try {
    const { data } = await axios.post<AutentifikacijaOdgovor>('/api/v1/autentifikacija/osvezi', {}, {
      withCredentials: true,
      headers: { 'X-Client-Signals': klijentskiSignali() }
    })
    accessToken = data.accessToken
    naNovuSesiju?.(data)
    return data
  } catch {
    accessToken = null
    return null
  }
}

async function obezbediIzazov(): Promise<void> {
  const { data: izazov } = await axios.get<Izazov>('/api/v1/bezbednost/izazov', { headers: { 'X-Client-Signals': klijentskiSignali() } })
  const resenje = await resiIzazov(izazov)
  const { data } = await axios.post<{ token: string; isticeUtc: string }>('/api/v1/bezbednost/izazov', { ...izazov, resenje }, {
    headers: { 'X-Client-Signals': klijentskiSignali() }
  })
  sacuvajTokenIzazova(data.token, data.isticeUtc)
}

type Ponovljeni = InternalAxiosRequestConfig & { _osvezeno?: boolean; _izazov?: boolean }

api.interceptors.response.use(
  (odgovor) => odgovor,
  async (greska: AxiosError<ProblemDetails>) => {
    const zahtev = greska.config as Ponovljeni | undefined
    const status = greska.response?.status
    if (!zahtev) throw greska

    // 428: Bot Detection traži rešen JS izazov -> rešavamo ga automatski i ponavljamo zahtev.
    if (status === 428 && !zahtev._izazov) {
      zahtev._izazov = true
      await obezbediIzazov()
      return api(zahtev as AxiosRequestConfig)
    }

    // 401: access token je istekao -> jedno osvežavanje, pa ponovni pokušaj.
    const jeAuthRuta = zahtev.url?.includes('/autentifikacija/')
    if (status === 401 && !zahtev._osvezeno && !jeAuthRuta) {
      zahtev._osvezeno = true
      const novi = await osveziSesiju()
      if (novi) return api(zahtev as AxiosRequestConfig)
      naIstekSesije?.()
    }

    throw greska
  }
)

/** Čitljiva poruka iz ProblemDetails odgovora (na srpskom, kako je vraća API). */
export function porukaGreske(greska: unknown): string {
  if (axios.isAxiosError<ProblemDetails>(greska)) {
    const p = greska.response?.data
    if (p?.greske) {
      return Object.values(p.greske).flat().join(' ')
    }
    if (p?.detail) return p.detail
    if (greska.response?.status === 429) return 'Previše zahteva. Sačekajte malo i pokušajte ponovo.'
    if (!greska.response) return 'Server nije dostupan. Proverite internet vezu.'
  }
  return 'Došlo je do neočekivane greške.'
}

/** Greške po poljima (za prikaz ispod polja forme). */
export function greskePolja(greska: unknown): Record<string, string> {
  if (axios.isAxiosError<ProblemDetails>(greska) && greska.response?.data?.greske) {
    return Object.fromEntries(Object.entries(greska.response.data.greske).map(([k, v]) => [k, v.join(' ')]))
  }
  return {}
}
