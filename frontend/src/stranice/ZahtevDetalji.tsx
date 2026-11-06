import { useState } from 'react'
import { useParams } from 'react-router'
import { porukaGreske } from '../api/klijent'
import { oglasi, zahtevi } from '../api/servisi'
import { useAuth } from '../kontekst/AuthKontekst'
import { useToast } from '../kontekst/ToastKontekst'
import { KarticaOglasa } from '../komponente/KarticaOglasa'
import { Greska, Modal, Prazno, Ucitavanje, Znacka } from '../komponente/Ui'
import { cena, relativno } from '../pomocno/format'
import { useUpit } from '../pomocno/kuke'

export default function ZahtevDetalji() {
  const id = Number(useParams().id)
  const { korisnik } = useAuth()
  const toast = useToast()
  const r = useUpit(() => zahtevi.detalji(id), [id])
  const [ponudi, setPonudi] = useState(false)
  const moji = useUpit(() => (korisnik ? oglasi.moji('Aktivan') : Promise.resolve(null)), [korisnik?.id])
  const [oglasId, setOglasId] = useState('')
  const [poruka, setPoruka] = useState('')

  if (r.ucitava) return <Ucitavanje />
  if (r.greska || !r.podaci) return <Greska poruka={r.greska ?? 'Zahtev ne postoji.'} />
  const { zahtev: z, odgovori, jeMoj } = r.podaci

  const izvrsi = async (fn: () => Promise<unknown>, tekst: string) => {
    try {
      await fn()
      toast.uspeh(tekst)
      setPonudi(false)
      await r.ponovo()
    } catch (e) {
      toast.greska(porukaGreske(e))
    }
  }

  return (
    <div className="space-y-6">
      <div className="kartica p-6">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <p className="text-sm text-slate-500">Tražim · {z.kategorijaNaziv}{z.gradNaziv && ` · ${z.gradNaziv}`}</p>
            <h1 className="text-2xl font-bold">{z.naslov}</h1>
          </div>
          <Znacka boja={z.status === 'Otvoren' ? 'bg-green-100 text-green-800' : 'bg-slate-100 text-slate-600'}>{z.status}</Znacka>
        </div>
        <p className="mt-3 whitespace-pre-line text-slate-700">{z.opis}</p>
        <p className="mt-3 text-sm text-slate-500">
          {z.maksimalnaCena && <>Maksimalna cena: <strong>{cena(z.maksimalnaCena)}</strong> · </>}{z.korisnikIme} · {relativno(z.kreiranoUtc)}
        </p>
        <div className="mt-4 flex flex-wrap gap-2">
          {korisnik && !jeMoj && z.status === 'Otvoren' && <button className="dugme-primarno" onClick={() => setPonudi(true)}>Ponudi svoj oglas</button>}
          {jeMoj && z.status === 'Otvoren' && (
            <>
              <button className="dugme-primarno" onClick={() => izvrsi(() => zahtevi.zatvori(id, true), 'Super! Zahtev je označen kao ispunjen.')}>Našao/la sam</button>
              <button className="dugme-sekundarno" onClick={() => izvrsi(() => zahtevi.zatvori(id, false), 'Zahtev je zatvoren.')}>Zatvori zahtev</button>
            </>
          )}
        </div>
      </div>
      <section>
        <h2 className="mb-3 text-lg font-bold">Ponuđeni oglasi ({odgovori.length})</h2>
        {odgovori.length ? (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {odgovori.map((o) => (
              <div key={o.id}>
                <KarticaOglasa oglas={o.oglas} prikaziStatus />
                {o.poruka && <p className="mt-1 text-xs text-slate-600">"{o.poruka}"</p>}
              </div>
            ))}
          </div>
        ) : <Prazno naslov="Još nema odgovora" />}
      </section>
      <Modal naslov="Ponudi oglas" otvoren={ponudi} zatvori={() => setPonudi(false)}>
        {moji.podaci?.stavke.length ? (
          <div className="space-y-3">
            <select className="unos" value={oglasId} onChange={(e) => setOglasId(e.target.value)}>
              <option value="">-- izaberite svoj aktivan oglas --</option>
              {moji.podaci.stavke.map((o) => <option key={o.id} value={o.id}>{o.naslov} ({cena(o.cena, o.tip)})</option>)}
            </select>
            <textarea className="unos" rows={2} maxLength={500} placeholder="Poruka (opciono)" value={poruka} onChange={(e) => setPoruka(e.target.value)} />
            <button className="dugme-primarno w-full" disabled={!oglasId} onClick={() => izvrsi(() => zahtevi.odgovori(id, Number(oglasId), poruka.trim() || undefined), 'Oglas je ponuđen.')}>
              Ponudi
            </button>
          </div>
        ) : <p className="text-sm text-slate-600">Nemate aktivnih oglasa koje biste mogli da ponudite.</p>}
      </Modal>
    </div>
  )
}
