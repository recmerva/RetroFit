// Klijentski deo Bot Detection zaštite.

/**
 * Grubi signali uređaja koji se šalju u zaglavlju X-Client-Signals. Namerno NE koristimo
 * canvas/audio/font otiske - ovi signali razlikuju klasu uređaja, a ne prate osobu.
 */
export function klijentskiSignali(): string {
  const n = navigator as Navigator & { webdriver?: boolean }
  return JSON.stringify({
    ekran: `${window.screen.width}x${window.screen.height}`,
    zona: Intl.DateTimeFormat().resolvedOptions().timeZone,
    jezik: navigator.language,
    platforma: (navigator as Navigator & { userAgentData?: { platform?: string } }).userAgentData?.platform ?? navigator.platform,
    gustinaPiksela: window.devicePixelRatio,
    dodir: navigator.maxTouchPoints > 0,
    webdriver: n.webdriver === true
  })
}

const KLJUC_TOKENA = 'retrofit_izazov'

export function tokenIzazova(): string | null {
  const sacuvan = sessionStorage.getItem(KLJUC_TOKENA)
  if (!sacuvan) return null
  const { token, isticeUtc } = JSON.parse(sacuvan) as { token: string; isticeUtc: string }
  return new Date(isticeUtc).getTime() > Date.now() + 10_000 ? token : null
}

export function sacuvajTokenIzazova(token: string, isticeUtc: string) {
  sessionStorage.setItem(KLJUC_TOKENA, JSON.stringify({ token, isticeUtc }))
}

interface Izazov {
  nonce: string
  tezina: number
  isticeUnix: number
  potpis: string
}

function vodeceNule(bajtovi: Uint8Array): number {
  let nule = 0
  for (const b of bajtovi) {
    if (b === 0) {
      nule += 8
      continue
    }
    nule += Math.clz32(b) - 24
    break
  }
  return nule
}

/**
 * Proof-of-work: traži broj X takav da SHA-256("nonce:X") počinje sa "tezina" nultih bitova.
 * Koristi Web Crypto API (crypto.subtle). Za težinu 16 to je u proseku ~65.000 heširanja (~1 s).
 */
export async function resiIzazov(izazov: Izazov): Promise<number> {
  const enkoder = new TextEncoder()
  for (let x = 0; ; x++) {
    const hes = new Uint8Array(await crypto.subtle.digest('SHA-256', enkoder.encode(`${izazov.nonce}:${x}`)))
    if (vodeceNule(hes) >= izazov.tezina) return x
    // Povremeno prepusti nit browser-u, da UI ne "zamrzne".
    if (x % 5000 === 0) await new Promise((r) => setTimeout(r, 0))
  }
}

export type { Izazov }
