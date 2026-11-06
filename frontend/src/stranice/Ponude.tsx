import { useState } from 'react'
import { Link } from 'react-router'
import { porukaGreske } from '../api/klijent'
import { ponude } from '../api/servisi'
import type { StatusPonude } from '../api/tipovi'
import { useAuth } from '../kontekst/AuthKontekst'
import { useToast } from '../kontekst/ToastKontekst'
import { SlikaIliZamena } from '../komponente/KarticaOglasa'
import { Greska, Kartice, Naslov, Prazno, Stranicenje, Ucitavanje, Znacka } from '../komponente/Ui'
import { cena, iznos, nazivStatusaPonude, relativno } from '../pomocno/format'
import { useUpit } from '../pomocno/kuke'

const boje: Record<StatusPonude, string> = {
  NaCekanju: 'bg-amber-100 text-amber-800',
  Prihvacena: 'bg-green-100 text-green-800',
  Odbijena: 'bg-red-100 text-red-800',
  Kontraponuda: 'bg-blue-100 text-blue-800',
  Istekla: 'bg-slate-200 text-slate-600'
}

export default function Ponude() {
  const { korisnik } = useAuth()
  const toast = useToast()
  const [kartica, setKartica] = useState<'primljene' | 'poslate'>('primljene')
  const [status, setStatus] = useState<StatusPonude | ''>('')
  const [stranica, setStranica] = useState(1)
  const r = useUpit(() => ponude.lista(kartica === 'poslate', status || undefined, stranica), [kartica, status, stranica])

  const akcija = async (fn: () => Promise<unknown>, poruka: string) => {
    try {
      await fn()
      toast.uspeh(poruka)
      await r.ponovo()
    } catch (e) {
      toast.greska(porukaGreske(e))
    }
  }

  return (
    <div>
      <Naslov>Ponude</Naslov>
      <Kartice aktivna={kartica} promeni={(k) => { setKartica(k); setStranica(1) }} kartice={[
        { kljuc: 'primljene', naziv: 'Za moje oglase' }, { kljuc: 'poslate', naziv: 'Moje ponude' }
      ]} />
      <select className="unos mb-4 w-auto" value={status} onChange={(e) => { setStatus(e.target.value as StatusPonude | ''); setStranica(1) }}>
        <option value="">Svi statusi</option>
        {(Object.keys(nazivStatusaPonude) as StatusPonude[]).map((s) => <option key={s} value={s}>{nazivStatusaPonude[s]}</option>)}
      </select>
      {r.ucitava ? <Ucitavanje /> : r.greska ? <Greska poruka={r.greska} ponovo={r.ponovo} /> : !r.podaci?.stavke.length ? (
        <Prazno naslov="Nema ponuda" opis={kartica === 'poslate' ? 'Ponudu možete dati na stranici oglasa tipa Prodaja.' : undefined} />
      ) : (
        <div className="space-y-3">
          {r.podaci.stavke.map((p) => {
            const mojRed = p.status === 'NaCekanju' && p.ponudioKorisnikId !== korisnik!.id
            return (
              <div key={p.id} className="kartica flex flex-wrap items-center gap-4 p-4">
                <Link to={`/oglasi/${p.oglasId}`} className="h-16 w-16 shrink-0 overflow-hidden rounded-lg">
                  <SlikaIliZamena putanja={p.oglasSlika} naslov={p.oglasNaslov} klasa="h-16" />
                </Link>
                <div className="min-w-0 flex-1">
                  <Link to={`/oglasi/${p.oglasId}`} className="font-semibold hover:underline">{p.oglasNaslov}</Link>
                  <p className="text-sm">
                    <strong className="text-retro-700">{iznos(p.iznos)}</strong>
                    <span className="text-slate-500"> (tražena cena {cena(p.cenaOglasa)}) · {p.ponudioKorisnikId === p.kupacId ? p.kupacIme : p.prodavacIme} · {relativno(p.kreiranoUtc)}</span>
                  </p>
                  {p.prethodnaPonudaId && <p className="text-xs text-blue-700">Kontraponuda</p>}
                  {p.napomena && <p className="text-xs text-slate-500">"{p.napomena}"</p>}
                </div>
                <Znacka boja={boje[p.status]}>{nazivStatusaPonude[p.status]}</Znacka>
                <div className="flex gap-2">
                  {mojRed && (
                    <>
                      <button className="dugme-primarno px-3 py-1" onClick={() => akcija(() => ponude.prihvati(p.id), 'Ponuda je prihvaćena. Oglas je rezervisan.')}>Prihvati</button>
                      <button className="dugme-sekundarno px-3 py-1" onClick={() => akcija(() => ponude.odbij(p.id), 'Ponuda je odbijena.')}>Odbij</button>
                    </>
                  )}
                  <Link to={`/razgovori/${p.razgovorId}`} className="dugme-sekundarno px-3 py-1">Razgovor</Link>
                </div>
              </div>
            )
          })}
          <Stranicenje stranica={r.podaci.stranica} ukupnoStranica={r.podaci.ukupnoStranica} promeni={setStranica} />
        </div>
      )}
    </div>
  )
}
