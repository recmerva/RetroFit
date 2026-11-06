import { useEffect, useState, type FormEvent } from 'react'
import { useNavigate, useParams } from 'react-router'
import { ArrowLeft, ArrowRight, ImagePlus, Trash2 } from 'lucide-react'
import { greskePolja, porukaGreske } from '../api/klijent'
import { katalog, oglasi } from '../api/servisi'
import type { DefinicijaAtributa, OglasPodaci, Slika, StanjeProizvoda, TipOglasa } from '../api/tipovi'
import { useToast } from '../kontekst/ToastKontekst'
import { Greska, Naslov, Polje, Ucitavanje } from '../komponente/Ui'
import { nazivStanja, nazivTipa, ravneKategorije } from '../pomocno/format'
import { IzborGrada } from '../komponente/IzborGrada'
import { useKategorije } from '../pomocno/kuke'

const MAKS_SLIKA = 10
const MAKS_VELICINA = 5 * 1024 * 1024
const DOZVOLJENI = ['image/jpeg', 'image/png', 'image/webp']

const prazan: OglasPodaci = {
  kategorijaId: 0, gradId: 0, naslov: '', opis: '', tip: 'Prodaja', cena: null, stanje: 'Polovno',
  dostavaMoguca: true, licnoPreuzimanjeMoguce: true, zeljenaZamena: '', atributi: {}, objaviOdmah: true
}

function validiraj(p: OglasPodaci, definicije: DefinicijaAtributa[]) {
  const g: Record<string, string> = {}
  if (!p.kategorijaId) g.kategorijaId = 'Izaberite kategoriju.'
  if (!p.gradId) g.gradId = 'Izaberite grad.'
  if (p.naslov.trim().length < 5) g.naslov = 'Naslov mora imati najmanje 5 karaktera.'
  if (p.opis.trim().length < 10) g.opis = 'Opis mora imati najmanje 10 karaktera.'
  if (p.tip === 'Prodaja' && !(Number(p.cena) > 0)) g.cena = 'Za prodaju je obavezna cena veća od 0.'
  if (!p.dostavaMoguca && !p.licnoPreuzimanjeMoguce) g.nacinPreuzimanja = 'Omogućite bar dostavu ili lično preuzimanje.'
  definicije.forEach((d) => {
    if (d.obavezan && !p.atributi?.[d.id]) g[`atribut-${d.id}`] = `${d.naziv} je obavezan.`
  })
  return g
}

export default function FormaOglasa() {
  const parametar = useParams().id
  const id = parametar ? Number(parametar) : null
  const navigacija = useNavigate()
  const toast = useToast()
  const kategorije = useKategorije()
  const [p, setP] = useState<OglasPodaci>(prazan)
  const [definicije, setDefinicije] = useState<DefinicijaAtributa[]>([])
  const [slike, setSlike] = useState<Slika[]>([])
  const [noveSlike, setNoveSlike] = useState<File[]>([])
  const [greske, setGreske] = useState<Record<string, string>>({})
  const [ucitava, setUcitava] = useState(!!id)
  const [greskaUcitavanja, setGreskaUcitavanja] = useState<string | null>(null)
  const [salje, setSalje] = useState(false)

  useEffect(() => {
    if (!id) return
    oglasi.detalji(id).then((o) => {
      if (!o.jeMoj) throw new Error('Možete menjati samo sopstvene oglase.')
      setP({
        kategorijaId: o.kategorijaId, gradId: o.gradId, naslov: o.naslov, opis: o.opis, tip: o.tip, cena: o.cena ?? null, stanje: o.stanje,
        dostavaMoguca: o.dostavaMoguca, licnoPreuzimanjeMoguce: o.licnoPreuzimanjeMoguce, zeljenaZamena: o.zeljenaZamena ?? '',
        atributi: Object.fromEntries(o.atributi.map((a) => [a.definicijaAtributaId, a.vrednost]))
      })
      setSlike(o.slike)
    }).catch((e) => setGreskaUcitavanja(e instanceof Error && !('isAxiosError' in e) ? e.message : porukaGreske(e)))
      .finally(() => setUcitava(false))
  }, [id])

  useEffect(() => {
    if (!p.kategorijaId) {
      setDefinicije([])
      return
    }
    katalog.atributi(p.kategorijaId).then(setDefinicije).catch(() => setDefinicije([]))
  }, [p.kategorijaId])

  if (ucitava) return <Ucitavanje />
  if (greskaUcitavanja) return <Greska poruka={greskaUcitavanja} />

  const izaberiSlike = (fajlovi: FileList | null) => {
    if (!fajlovi) return
    const lista = [...fajlovi]
    const losi = lista.filter((f) => !DOZVOLJENI.includes(f.type) || f.size > MAKS_VELICINA)
    if (losi.length) toast.greska('Dozvoljene su samo JPEG, PNG i WebP slike do 5 MB.')
    const dobri = lista.filter((f) => !losi.includes(f))
    const mesta = MAKS_SLIKA - slike.length - noveSlike.length
    if (dobri.length > mesta) toast.greska(`Možete dodati još najviše ${mesta} slika.`)
    setNoveSlike((n) => [...n, ...dobri.slice(0, Math.max(0, mesta))])
  }

  const pomeri = async (indeks: number, smer: -1 | 1) => {
    if (!id) return
    const novi = [...slike]
    const cilj = indeks + smer
    if (cilj < 0 || cilj >= novi.length) return
    ;[novi[indeks], novi[cilj]] = [novi[cilj], novi[indeks]]
    setSlike(novi)
    try {
      await oglasi.redosledSlika(id, novi.map((s) => s.id))
    } catch (e) {
      toast.greska(porukaGreske(e))
      setSlike(slike)
    }
  }

  const ukloniSliku = async (slikaId: number) => {
    if (!id) return
    try {
      await oglasi.ukloniSliku(id, slikaId)
      setSlike((s) => s.filter((x) => x.id !== slikaId))
    } catch (e) {
      toast.greska(porukaGreske(e))
    }
  }

  const posalji = async (e: FormEvent) => {
    e.preventDefault()
    const g = validiraj(p, definicije)
    setGreske(g)
    if (Object.keys(g).length) {
      toast.greska('Proverite označena polja.')
      return
    }
    setSalje(true)
    try {
      const telo: OglasPodaci = {
        ...p,
        cena: p.tip === 'Prodaja' ? Number(p.cena) : null,
        zeljenaZamena: p.tip === 'Razmena' ? p.zeljenaZamena || null : null,
        // Šalju se samo atributi koji pripadaju izabranoj kategoriji.
        atributi: Object.fromEntries(definicije.map((d) => [d.id, p.atributi?.[d.id] || null]))
      }
      const oglas = id ? await oglasi.izmeni(id, telo) : await oglasi.kreiraj(telo)
      if (noveSlike.length) await oglasi.dodajSlike(oglas.id, noveSlike)
      toast.uspeh(id ? 'Oglas je sačuvan.' : 'Oglas je kreiran.')
      navigacija(`/oglasi/${oglas.id}`)
    } catch (err) {
      setGreske(greskePolja(err))
      toast.greska(porukaGreske(err))
    } finally {
      setSalje(false)
    }
  }

  const postavi = <K extends keyof OglasPodaci>(k: K, v: OglasPodaci[K]) => setP((x) => ({ ...x, [k]: v }))
  const postaviAtribut = (defId: number, v: string) => setP((x) => ({ ...x, atributi: { ...x.atributi, [defId]: v } }))

  return (
    <form onSubmit={posalji} className="mx-auto max-w-3xl space-y-6" noValidate>
      <Naslov>{id ? 'Izmena oglasa' : 'Novi oglas'}</Naslov>

      <section className="kartica space-y-4 p-4 sm:p-6">
        <h2 className="font-semibold">Osnovni podaci</h2>
        <Polje oznaka="Naslov" greska={greske.naslov}>
          <input className="unos" maxLength={120} value={p.naslov} onChange={(e) => postavi('naslov', e.target.value)} placeholder="npr. Gradski bicikl Capriolo, veličina M" />
        </Polje>
        <div className="grid gap-4 sm:grid-cols-2">
          <Polje oznaka="Kategorija" greska={greske.kategorijaId}>
            <select className="unos" value={p.kategorijaId || ''} onChange={(e) => setP((x) => ({ ...x, kategorijaId: Number(e.target.value), atributi: {} }))}>
              <option value="">-- izaberite podkategoriju --</option>
              {ravneKategorije(kategorije).map((k) => <option key={k.id} value={k.id} disabled={!k.jeList}>{k.naziv}</option>)}
            </select>
          </Polje>
          <Polje oznaka="Grad" greska={greske.gradId}>
            <IzborGrada vrednost={p.gradId || ''} promeni={(gradId) => postavi('gradId', Number(gradId))} placeholder="Upišite i izaberite grad..." />
          </Polje>
        </div>
        <Polje oznaka="Opis" greska={greske.opis}>
          <textarea className="unos" rows={6} maxLength={4000} value={p.opis} onChange={(e) => postavi('opis', e.target.value)}
            placeholder="Opišite stanje, starost, razlog prodaje, šta ide uz predmet..." />
        </Polje>
      </section>

      <section className="kartica space-y-4 p-4 sm:p-6">
        <h2 className="font-semibold">Cena i stanje</h2>
        <div className="flex flex-wrap gap-2">
          {(Object.keys(nazivTipa) as TipOglasa[]).map((t) => (
            <button type="button" key={t} onClick={() => postavi('tip', t)}
              className={p.tip === t ? 'dugme-primarno' : 'dugme-sekundarno'}>{nazivTipa[t]}</button>
          ))}
        </div>
        <div className="grid gap-4 sm:grid-cols-2">
          {p.tip === 'Prodaja' && (
            <Polje oznaka="Cena (RSD)" greska={greske.cena}>
              <input className="unos" type="number" min={1} step="1" value={p.cena ?? ''} onChange={(e) => postavi('cena', e.target.value ? Number(e.target.value) : null)} />
            </Polje>
          )}
          {p.tip === 'Razmena' && (
            <Polje oznaka="Šta želite u zamenu?">
              <input className="unos" maxLength={300} value={p.zeljenaZamena ?? ''} onChange={(e) => postavi('zeljenaZamena', e.target.value)} />
            </Polje>
          )}
          <Polje oznaka="Stanje predmeta">
            <select className="unos" value={p.stanje} onChange={(e) => postavi('stanje', e.target.value as StanjeProizvoda)}>
              {(Object.keys(nazivStanja) as StanjeProizvoda[]).map((s) => <option key={s} value={s}>{nazivStanja[s]}</option>)}
            </select>
          </Polje>
        </div>
        <div className="flex flex-wrap gap-6 text-sm">
          <label className="flex items-center gap-2">
            <input type="checkbox" className="accent-retro-600" checked={p.dostavaMoguca} onChange={(e) => postavi('dostavaMoguca', e.target.checked)} /> Šaljem brzom poštom
          </label>
          <label className="flex items-center gap-2">
            <input type="checkbox" className="accent-retro-600" checked={p.licnoPreuzimanjeMoguce} onChange={(e) => postavi('licnoPreuzimanjeMoguce', e.target.checked)} /> Lično preuzimanje
          </label>
        </div>
        {greske.nacinPreuzimanja && <p className="text-xs text-red-600">{greske.nacinPreuzimanja}</p>}
      </section>

      {definicije.length > 0 && (
        <section className="kartica space-y-4 p-4 sm:p-6">
          <h2 className="font-semibold">Detalji za kategoriju</h2>
          <div className="grid gap-4 sm:grid-cols-2">
            {definicije.map((d) => {
              const vrednost = p.atributi?.[d.id] ?? ''
              const oznaka = `${d.naziv}${d.jedinicaMere ? ` (${d.jedinicaMere})` : ''}${d.obavezan ? ' *' : ''}`
              return (
                <Polje key={d.id} oznaka={oznaka} greska={greske[`atribut-${d.id}`]}>
                  {d.tip === 'Izbor' ? (
                    <select className="unos" value={vrednost} onChange={(e) => postaviAtribut(d.id, e.target.value)}>
                      <option value="">--</option>
                      {d.opcije.map((op) => <option key={op} value={op}>{op}</option>)}
                    </select>
                  ) : d.tip === 'DaNe' ? (
                    <select className="unos" value={vrednost} onChange={(e) => postaviAtribut(d.id, e.target.value)}>
                      <option value="">--</option>
                      <option value="da">Da</option>
                      <option value="ne">Ne</option>
                    </select>
                  ) : (
                    <input className="unos" type={d.tip === 'Broj' ? 'number' : 'text'} maxLength={200} value={vrednost}
                      onChange={(e) => postaviAtribut(d.id, e.target.value)} />
                  )}
                </Polje>
              )
            })}
          </div>
        </section>
      )}

      <section className="kartica space-y-4 p-4 sm:p-6">
        <h2 className="font-semibold">Slike ({slike.length + noveSlike.length}/{MAKS_SLIKA})</h2>
        <p className="text-xs text-slate-500">Prva slika je glavna. JPEG, PNG ili WebP, do 5 MB po slici.</p>
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
          {slike.map((s, i) => (
            <div key={s.id} className="relative overflow-hidden rounded-lg border border-slate-200">
              <img src={s.putanja} alt="" className="h-28 w-full object-cover" />
              {i === 0 && <span className="znacka absolute top-1 left-1 bg-retro-600 text-white">Glavna</span>}
              <div className="flex justify-between bg-white p-1">
                <button type="button" onClick={() => pomeri(i, -1)} disabled={i === 0} aria-label="Pomeri levo"><ArrowLeft className="h-4 w-4" /></button>
                <button type="button" onClick={() => ukloniSliku(s.id)} aria-label="Ukloni"><Trash2 className="h-4 w-4 text-red-600" /></button>
                <button type="button" onClick={() => pomeri(i, 1)} disabled={i === slike.length - 1} aria-label="Pomeri desno"><ArrowRight className="h-4 w-4" /></button>
              </div>
            </div>
          ))}
          {noveSlike.map((f, i) => (
            <div key={`${f.name}-${i}`} className="relative overflow-hidden rounded-lg border-2 border-dashed border-retro-300">
              <img src={URL.createObjectURL(f)} alt="" className="h-28 w-full object-cover" />
              <button type="button" className="absolute top-1 right-1 rounded-full bg-white p-1 shadow" onClick={() => setNoveSlike((n) => n.filter((_, j) => j !== i))}
                aria-label="Ukloni"><Trash2 className="h-4 w-4 text-red-600" /></button>
            </div>
          ))}
          {slike.length + noveSlike.length < MAKS_SLIKA && (
            <label className="flex h-36 cursor-pointer flex-col items-center justify-center gap-1 rounded-lg border-2 border-dashed border-slate-300 text-sm text-slate-500 hover:border-retro-400">
              <ImagePlus className="h-6 w-6" /> Dodaj slike
              <input type="file" accept={DOZVOLJENI.join(',')} multiple className="hidden" onChange={(e) => { izaberiSlike(e.target.files); e.target.value = '' }} />
            </label>
          )}
        </div>
      </section>

      <div className="flex flex-wrap items-center justify-between gap-3">
        {!id ? (
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" className="accent-retro-600" checked={p.objaviOdmah ?? true} onChange={(e) => postavi('objaviOdmah', e.target.checked)} />
            Objavi odmah (u suprotnom se čuva kao nacrt)
          </label>
        ) : <span />}
        <div className="flex gap-2">
          <button type="button" className="dugme-sekundarno" onClick={() => navigacija(-1)}>Odustani</button>
          <button className="dugme-primarno" disabled={salje}>{salje ? 'Čuvanje...' : id ? 'Sačuvaj izmene' : 'Postavi oglas'}</button>
        </div>
      </div>
    </form>
  )
}
