import type * as T from '../api/tipovi'

const brojevi = new Intl.NumberFormat('sr-Latn-RS', { maximumFractionDigits: 0 })

export function cena(iznos: number | undefined | null, tip?: T.TipOglasa): string {
  if (tip === 'Poklon') return 'Poklanja se'
  if (tip === 'Razmena') return 'Razmena'
  if (iznos === undefined || iznos === null) return 'Po dogovoru'
  return `${brojevi.format(iznos)} RSD`
}

export function iznos(vrednost: number): string {
  return `${brojevi.format(vrednost)} RSD`
}

export function datum(utc?: string): string {
  if (!utc) return '-'
  return new Date(utc).toLocaleDateString('sr-Latn-RS', { day: 'numeric', month: 'numeric', year: 'numeric' })
}

export function datumVreme(utc?: string): string {
  if (!utc) return '-'
  return new Date(utc).toLocaleString('sr-Latn-RS', { day: 'numeric', month: 'numeric', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

/** "pre 5 min", "pre 3 dana"... */
export function relativno(utc?: string): string {
  if (!utc) return ''
  const s = Math.round((Date.now() - new Date(utc).getTime()) / 1000)
  if (s < 0) {
    const u = -s
    if (u < 3600) return `za ${Math.max(1, Math.round(u / 60))} min`
    if (u < 86400) return `za ${Math.round(u / 3600)} h`
    return `za ${Math.round(u / 86400)} d`
  }
  if (s < 60) return 'upravo'
  if (s < 3600) return `pre ${Math.round(s / 60)} min`
  if (s < 86400) return `pre ${Math.round(s / 3600)} h`
  if (s < 86400 * 30) return `pre ${Math.round(s / 86400)} d`
  return datum(utc)
}

export const nazivStanja: Record<T.StanjeProizvoda, string> = {
  Novo: 'Novo',
  KaoNovo: 'Kao novo',
  Polovno: 'Polovno',
  ZaDelove: 'Za delove'
}

export const nazivTipa: Record<T.TipOglasa, string> = { Prodaja: 'Prodaja', Razmena: 'Razmena', Poklon: 'Poklon' }

export const nazivStatusa: Record<T.StatusOglasa, string> = {
  Nacrt: 'Nacrt',
  Aktivan: 'Aktivan',
  Rezervisan: 'Rezervisan',
  Prodat: 'Prodat',
  Istekao: 'Istekao',
  Uklonjen: 'Uklonjen'
}

export const bojaStatusa: Record<T.StatusOglasa, string> = {
  Nacrt: 'bg-slate-100 text-slate-700',
  Aktivan: 'bg-green-100 text-green-800',
  Rezervisan: 'bg-amber-100 text-amber-800',
  Prodat: 'bg-blue-100 text-blue-800',
  Istekao: 'bg-slate-200 text-slate-600',
  Uklonjen: 'bg-red-100 text-red-800'
}

export const nazivStatusaPonude: Record<T.StatusPonude, string> = {
  NaCekanju: 'Na čekanju',
  Prihvacena: 'Prihvaćena',
  Odbijena: 'Odbijena',
  Kontraponuda: 'Kontraponuda',
  Istekla: 'Istekla'
}

export const nazivStatusaRezervacije: Record<T.StatusRezervacije, string> = {
  Aktivna: 'Aktivna',
  Realizovana: 'Realizovana',
  Otkazana: 'Otkazana',
  Istekla: 'Istekla'
}

export const nazivStatusaTransakcije: Record<T.StatusTransakcije, string> = { UToku: 'U toku', Zavrsena: 'Završena', Otkazana: 'Otkazana' }

export const nazivEscrowa: Record<T.StatusEscrowa, string> = {
  NijeKoriscen: 'Bez escrow-a',
  CekaUplatu: 'Čeka uplatu',
  Deponovano: 'Novac deponovan',
  Isplaceno: 'Isplaćeno prodavcu',
  Vraceno: 'Vraćeno kupcu'
}

export const nazivRazloga: Record<T.RazlogPrijave, string> = {
  Prevara: 'Prevara',
  ZabranjenSadrzaj: 'Zabranjen sadržaj',
  PogresnaKategorija: 'Pogrešna kategorija',
  Duplikat: 'Duplikat',
  Spam: 'Spam',
  UvredljivSadrzaj: 'Uvredljiv sadržaj',
  Ostalo: 'Ostalo'
}

export const nazivBedza: Record<string, string> = {
  VerifikovanTelefon: 'Verifikovan telefon',
  PouzdanProdavac: 'Pouzdan prodavac',
  VisegodisnjiClan: 'Višegodišnji član'
}

export const sortiranja: { vrednost: T.SortiranjeOglasa; naziv: string }[] = [
  { vrednost: 'Najnovije', naziv: 'Najnovije' },
  { vrednost: 'CenaRastuce', naziv: 'Cena: od najniže' },
  { vrednost: 'CenaOpadajuce', naziv: 'Cena: od najviše' },
  { vrednost: 'Popularnost', naziv: 'Najpopularnije' },
  { vrednost: 'Udaljenost', naziv: 'Najbliže' }
]

/** Ravna lista kategorija (glavne + podkategorije) za padajuće menije. */
export function ravneKategorije(stablo: T.Kategorija[]): { id: number; naziv: string; jeList: boolean }[] {
  return stablo.flatMap((k) => [
    { id: k.id, naziv: k.naziv, jeList: k.podkategorije.length === 0 },
    ...k.podkategorije.map((p) => ({ id: p.id, naziv: `${k.naziv} › ${p.naziv}`, jeList: true }))
  ])
}
