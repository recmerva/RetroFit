import { useCallback, useEffect, useState } from 'react'
import { useSearchParams } from 'react-router'
import { BellPlus, LocateFixed, SlidersHorizontal, X } from 'lucide-react'
import { porukaGreske } from '../api/klijent'
import { oglasi, sacuvanePretrage, type ParametriPretrage } from '../api/servisi'
import type { OglasKartica, SortiranjeOglasa, StanjeProizvoda, TipOglasa } from '../api/tipovi'
import { useAuth } from '../kontekst/AuthKontekst'
import { useToast } from '../kontekst/ToastKontekst'
import { MrezaOglasa } from '../komponente/KarticaOglasa'
import { Greska, Modal, Prazno, Stranicenje, Ucitavanje } from '../komponente/Ui'
import { nazivStanja, nazivTipa, ravneKategorije, sortiranja } from '../pomocno/format'
import { useGradovi, useKategorije, useUpit } from '../pomocno/kuke'
import { usePrebaciOmiljeni } from '../pomocno/omiljeni'

const broj = (v: string | null) => (v ? Number(v) : undefined)

export default function Oglasi() {
  const [parametri, setParametri] = useSearchParams()
  const { korisnik } = useAuth()
  const toast = useToast()
  const kategorije = useKategorije()
  const gradovi = useGradovi()
  const [filteriOtvoreni, setFilteriOtvoreni] = useState(false)
  const [cuvanje, setCuvanje] = useState(false)
  const [nazivPretrage, setNazivPretrage] = useState('')
  const [upitUnos, setUpitUnos] = useState(parametri.get('upit') ?? '')

  useEffect(() => setUpitUnos(parametri.get('upit') ?? ''), [parametri])

  const p: ParametriPretrage = {
    upit: parametri.get('upit') ?? undefined,
    kategorijaId: broj(parametri.get('kategorijaId')),
    gradId: broj(parametri.get('gradId')),
    cenaOd: broj(parametri.get('cenaOd')),
    cenaDo: broj(parametri.get('cenaDo')),
    stanja: parametri.getAll('stanja') as StanjeProizvoda[],
    tip: (parametri.get('tip') as TipOglasa) || undefined,
    samoSaDostavom: parametri.get('samoSaDostavom') === 'true' || undefined,
    lat: broj(parametri.get('lat')),
    lng: broj(parametri.get('lng')),
    radijusKm: broj(parametri.get('radijusKm')),
    sortiranje: (parametri.get('sortiranje') as SortiranjeOglasa) || 'Najnovije',
    stranica: broj(parametri.get('stranica')) ?? 1,
    velicina: 20
  }
  const kljuc = parametri.toString()
  const rezultat = useUpit(() => oglasi.pretraga(p), [kljuc])

  const azuriraj = useCallback(
    (fn: (o: OglasKartica) => OglasKartica) => rezultat.setPodaci((s) => (s ? { ...s, stavke: s.stavke.map(fn) } : s)),
    [rezultat]
  )
  const promeniOmiljeni = usePrebaciOmiljeni(azuriraj)

  const postavi = (izmene: Record<string, string | string[] | undefined>, zadrziStranicu = false) => {
    const novi = new URLSearchParams(parametri)
    Object.entries(izmene).forEach(([k, v]) => {
      novi.delete(k)
      if (Array.isArray(v)) v.forEach((x) => novi.append(k, x))
      else if (v !== undefined && v !== '') novi.set(k, v)
    })
    if (!zadrziStranicu) novi.delete('stranica')
    setParametri(novi)
  }

  const mojaLokacija = () => {
    if (!navigator.geolocation) {
      toast.greska('Pregledač ne podržava geolokaciju.')
      return
    }
    navigator.geolocation.getCurrentPosition(
      (poz) => postavi({ lat: poz.coords.latitude.toFixed(4), lng: poz.coords.longitude.toFixed(4), radijusKm: p.radijusKm?.toString() ?? '50', sortiranje: 'Udaljenost' }),
      () => toast.greska('Lokacija nije dostupna. Izaberite grad i radijus.')
    )
  }

  const sacuvajPretragu = async () => {
    try {
      await sacuvanePretrage.sacuvaj({
        naziv: nazivPretrage || p.upit || 'Moja pretraga',
        upit: p.upit, kategorijaId: p.kategorijaId, gradId: p.gradId, cenaOd: p.cenaOd, cenaDo: p.cenaDo,
        stanje: p.stanja?.[0], tip: p.tip, obavestavaj: true
      })
      toast.uspeh('Pretraga je sačuvana. Obavestićemo vas o novim oglasima.')
      setCuvanje(false)
    } catch (e) {
      toast.greska(porukaGreske(e))
    }
  }

  const imaFiltera = [...parametri.keys()].some((k) => !['stranica', 'sortiranje'].includes(k))

  const filteri = (
    <div className="space-y-4">
      <div>
        <span className="oznaka">Kategorija</span>
        <select className="unos" value={p.kategorijaId ?? ''} onChange={(e) => postavi({ kategorijaId: e.target.value })}>
          <option value="">Sve kategorije</option>
          {ravneKategorije(kategorije).map((k) => <option key={k.id} value={k.id}>{k.naziv}</option>)}
        </select>
      </div>
      <div>
        <span className="oznaka">Grad</span>
        <select className="unos" value={p.gradId ?? ''} onChange={(e) => postavi({ gradId: e.target.value, lat: undefined, lng: undefined })}>
          <option value="">Svi gradovi</option>
          {gradovi.map((g) => <option key={g.id} value={g.id}>{g.naziv}</option>)}
        </select>
      </div>
      <div>
        <span className="oznaka">Udaljenost</span>
        <div className="flex gap-2">
          <select className="unos" value={p.radijusKm ?? ''} onChange={(e) => postavi({ radijusKm: e.target.value })}
            disabled={!p.gradId && !p.lat}>
            <option value="">Samo izabrani grad</option>
            {[10, 25, 50, 100, 200].map((r) => <option key={r} value={r}>do {r} km</option>)}
          </select>
          <button className="dugme-sekundarno px-2" onClick={mojaLokacija} title="Koristi moju lokaciju" type="button">
            <LocateFixed className="h-4 w-4" />
          </button>
        </div>
        {p.lat && <p className="mt-1 text-xs text-slate-500">Koristi se vaša trenutna lokacija.</p>}
      </div>
      <div>
        <span className="oznaka">Cena (RSD)</span>
        <div className="flex gap-2">
          <input className="unos" type="number" min={0} placeholder="od" defaultValue={p.cenaOd}
            onBlur={(e) => postavi({ cenaOd: e.target.value })} key={`od-${p.cenaOd}`} />
          <input className="unos" type="number" min={0} placeholder="do" defaultValue={p.cenaDo}
            onBlur={(e) => postavi({ cenaDo: e.target.value })} key={`do-${p.cenaDo}`} />
        </div>
      </div>
      <fieldset>
        <legend className="oznaka">Stanje</legend>
        {(Object.keys(nazivStanja) as StanjeProizvoda[]).map((s) => (
          <label key={s} className="flex items-center gap-2 py-0.5 text-sm">
            <input type="checkbox" checked={p.stanja?.includes(s) ?? false} className="accent-retro-600"
              onChange={(e) => postavi({ stanja: e.target.checked ? [...(p.stanja ?? []), s] : (p.stanja ?? []).filter((x) => x !== s) })} />
            {nazivStanja[s]}
          </label>
        ))}
      </fieldset>
      <div>
        <span className="oznaka">Tip oglasa</span>
        <select className="unos" value={p.tip ?? ''} onChange={(e) => postavi({ tip: e.target.value })}>
          <option value="">Svi tipovi</option>
          {(Object.keys(nazivTipa) as TipOglasa[]).map((t) => <option key={t} value={t}>{nazivTipa[t]}</option>)}
        </select>
      </div>
      <label className="flex items-center gap-2 text-sm">
        <input type="checkbox" className="accent-retro-600" checked={p.samoSaDostavom ?? false}
          onChange={(e) => postavi({ samoSaDostavom: e.target.checked ? 'true' : undefined })} />
        Samo sa dostavom
      </label>
      {imaFiltera && (
        <button className="dugme-sekundarno w-full" onClick={() => setParametri(new URLSearchParams())}>
          <X className="h-4 w-4" /> Poništi filtere
        </button>
      )}
    </div>
  )

  return (
    <div>
      <div className="mb-4 flex flex-wrap items-center gap-3">
        <form className="flex flex-1 gap-2" onSubmit={(e) => { e.preventDefault(); postavi({ upit: upitUnos.trim() }) }} role="search">
          <input className="unos" placeholder="Pretraga po naslovu i opisu..." value={upitUnos} onChange={(e) => setUpitUnos(e.target.value)}
            aria-label="Pretraga" />
          <button className="dugme-primarno">Traži</button>
        </form>
        <select className="unos w-auto" value={p.sortiranje} onChange={(e) => postavi({ sortiranje: e.target.value })} aria-label="Sortiranje">
          {sortiranja.map((s) => <option key={s.vrednost} value={s.vrednost} disabled={s.vrednost === 'Udaljenost' && !p.gradId && !p.lat}>{s.naziv}</option>)}
        </select>
        <button className="dugme-sekundarno lg:hidden" onClick={() => setFilteriOtvoreni(true)}>
          <SlidersHorizontal className="h-4 w-4" /> Filteri
        </button>
        {korisnik && imaFiltera && (
          <button className="dugme-sekundarno" onClick={() => setCuvanje(true)}>
            <BellPlus className="h-4 w-4" /> Sačuvaj pretragu
          </button>
        )}
      </div>

      <div className="grid gap-6 lg:grid-cols-[260px_1fr]">
        <aside className="kartica hidden h-fit p-4 lg:block">{filteri}</aside>
        <section>
          {rezultat.ucitava ? <Ucitavanje /> : rezultat.greska ? <Greska poruka={rezultat.greska} ponovo={rezultat.ponovo} /> : rezultat.podaci && (
            <>
              <p className="mb-3 text-sm text-slate-500">Pronađeno oglasa: <strong>{rezultat.podaci.ukupnoStavki}</strong></p>
              {rezultat.podaci.stavke.length === 0 ? (
                <Prazno naslov="Nema oglasa za izabrane filtere" opis="Probajte sa manje filtera ili sačuvajte pretragu da vas obavestimo kad se pojavi nešto novo." />
              ) : (
                <MrezaOglasa oglasi={rezultat.podaci.stavke} promeniOmiljeni={promeniOmiljeni} />
              )}
              <Stranicenje stranica={rezultat.podaci.stranica} ukupnoStranica={rezultat.podaci.ukupnoStranica}
                promeni={(s) => { postavi({ stranica: String(s) }, true); window.scrollTo({ top: 0, behavior: 'smooth' }) }} />
            </>
          )}
        </section>
      </div>

      <Modal naslov="Filteri" otvoren={filteriOtvoreni} zatvori={() => setFilteriOtvoreni(false)}>{filteri}</Modal>
      <Modal naslov="Sačuvaj pretragu" otvoren={cuvanje} zatvori={() => setCuvanje(false)}>
        <p className="mb-3 text-sm text-slate-600">Dobićete obaveštenje kada se pojavi novi oglas koji odgovara ovim filterima.</p>
        <input className="unos mb-4" placeholder="Naziv pretrage (npr. Bicikl do 20.000)" value={nazivPretrage} onChange={(e) => setNazivPretrage(e.target.value)} />
        <button className="dugme-primarno w-full" onClick={sacuvajPretragu}>Sačuvaj</button>
      </Modal>
    </div>
  )
}
