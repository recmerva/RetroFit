import { useState } from 'react'
import { useNavigate } from 'react-router'
import { Bell, CheckCheck } from 'lucide-react'
import { obavestenja } from '../api/servisi'
import type { Obavestenje } from '../api/tipovi'
import { Greska, Naslov, Prazno, Stranicenje, Ucitavanje } from '../komponente/Ui'
import { relativno } from '../pomocno/format'
import { useUpit } from '../pomocno/kuke'

export default function Obavestenja() {
  const navigacija = useNavigate()
  const [samoNeprocitana, setSamoNeprocitana] = useState(false)
  const [stranica, setStranica] = useState(1)
  const r = useUpit(() => obavestenja.lista(samoNeprocitana, stranica), [samoNeprocitana, stranica])

  const otvori = async (o: Obavestenje) => {
    if (!o.procitanoUtc) {
      await obavestenja.procitano(o.id).catch(() => undefined)
      r.setPodaci((s) => s ? { ...s, stavke: s.stavke.map((x) => (x.id === o.id ? { ...x, procitanoUtc: new Date().toISOString() } : x)) } : s)
    }
    if (o.link) navigacija(o.link)
  }

  return (
    <div className="mx-auto max-w-3xl">
      <Naslov akcija={
        <div className="flex gap-2">
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" className="accent-retro-600" checked={samoNeprocitana} onChange={(e) => { setSamoNeprocitana(e.target.checked); setStranica(1) }} />
            Samo nepročitana
          </label>
          <button className="dugme-sekundarno" onClick={async () => { await obavestenja.svaProcitana(); await r.ponovo() }}>
            <CheckCheck className="h-4 w-4" /> Označi sve kao pročitano
          </button>
        </div>
      }>Obaveštenja</Naslov>
      {r.ucitava ? <Ucitavanje /> : r.greska ? <Greska poruka={r.greska} ponovo={r.ponovo} /> : r.podaci?.stavke.length ? (
        <>
          <ul className="kartica divide-y divide-slate-100">
            {r.podaci.stavke.map((o) => (
              <li key={o.id}>
                <button onClick={() => otvori(o)} className={`flex w-full gap-3 p-4 text-left hover:bg-slate-50 ${o.procitanoUtc ? '' : 'bg-retro-50/60'}`}>
                  <Bell className={`mt-0.5 h-5 w-5 shrink-0 ${o.procitanoUtc ? 'text-slate-300' : 'text-retro-600'}`} />
                  <span className="flex-1">
                    <span className={`block text-sm ${o.procitanoUtc ? '' : 'font-semibold'}`}>{o.naslov}</span>
                    <span className="block text-sm text-slate-600">{o.sadrzaj}</span>
                    <span className="text-xs text-slate-400">{relativno(o.kreiranoUtc)}</span>
                  </span>
                </button>
              </li>
            ))}
          </ul>
          <Stranicenje stranica={r.podaci.stranica} ukupnoStranica={r.podaci.ukupnoStranica} promeni={setStranica} />
        </>
      ) : <Prazno naslov="Nema obaveštenja" />}
    </div>
  )
}
