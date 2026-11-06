import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { Plus, Search } from 'lucide-react'
import { greskePolja, porukaGreske } from '../api/klijent'
import { zahtevi } from '../api/servisi'
import { useAuth } from '../kontekst/AuthKontekst'
import { useToast } from '../kontekst/ToastKontekst'
import { Greska, Kartice, Modal, Naslov, Polje, Prazno, Stranicenje, Ucitavanje, Znacka } from '../komponente/Ui'
import { cena, ravneKategorije, relativno } from '../pomocno/format'
import { IzborGrada } from '../komponente/IzborGrada'
import { useKategorije, useUpit } from '../pomocno/kuke'

export default function Trazim() {
  const { korisnik } = useAuth()
  const toast = useToast()
  const navigacija = useNavigate()
  const kategorije = useKategorije()
  const [kartica, setKartica] = useState<'svi' | 'moji'>('svi')
  const [filter, setFilter] = useState({ upit: '', kategorijaId: '', gradId: '' })
  const [primenjen, setPrimenjen] = useState(filter)
  const [stranica, setStranica] = useState(1)
  const [novi, setNovi] = useState(false)
  const [z, setZ] = useState({ naslov: '', opis: '', kategorijaId: '', gradId: '', maksimalnaCena: '' })
  const [greske, setGreske] = useState<Record<string, string>>({})

  const r = useUpit(() => kartica === 'moji' ? zahtevi.moji() : zahtevi.pretraga({
    upit: primenjen.upit || undefined, kategorijaId: primenjen.kategorijaId ? Number(primenjen.kategorijaId) : undefined,
    gradId: primenjen.gradId ? Number(primenjen.gradId) : undefined, stranica
  }), [kartica, primenjen, stranica])

  const kreiraj = async (e: FormEvent) => {
    e.preventDefault()
    const g: Record<string, string> = {}
    if (z.naslov.trim().length < 5) g.naslov = 'Naslov mora imati najmanje 5 karaktera.'
    if (z.opis.trim().length < 10) g.opis = 'Opis mora imati najmanje 10 karaktera.'
    if (!z.kategorijaId) g.kategorijaId = 'Izaberite kategoriju.'
    setGreske(g)
    if (Object.keys(g).length) return
    try {
      const novo = await zahtevi.kreiraj({
        naslov: z.naslov.trim(), opis: z.opis.trim(), kategorijaId: Number(z.kategorijaId),
        gradId: z.gradId ? Number(z.gradId) : undefined, maksimalnaCena: z.maksimalnaCena ? Number(z.maksimalnaCena) : undefined
      })
      toast.uspeh('Zahtev je objavljen.')
      navigacija(`/trazim/${novo.id}`)
    } catch (err) {
      setGreske(greskePolja(err))
      toast.greska(porukaGreske(err))
    }
  }

  return (
    <div>
      <Naslov akcija={korisnik && <button className="dugme-primarno" onClick={() => setNovi(true)}><Plus className="h-4 w-4" /> Objavi "Tražim..."</button>}>
        Tražim...
      </Naslov>
      <p className="-mt-4 mb-6 text-sm text-slate-600">Ne možete da nađete ono što vam treba? Objavite šta tražite, a prodavci će vam ponuditi svoje oglase.</p>
      {korisnik && <Kartice aktivna={kartica} promeni={(k) => { setKartica(k); setStranica(1) }} kartice={[{ kljuc: 'svi', naziv: 'Svi zahtevi' }, { kljuc: 'moji', naziv: 'Moji zahtevi' }]} />}
      {kartica === 'svi' && (
        <form className="mb-4 grid gap-2 sm:grid-cols-2 lg:grid-cols-[1fr_220px_220px_auto]" onSubmit={(e) => { e.preventDefault(); setPrimenjen(filter); setStranica(1) }}>
          <input className="unos" placeholder="Pretraga..." value={filter.upit} onChange={(e) => setFilter({ ...filter, upit: e.target.value })} />
          <select className="unos" value={filter.kategorijaId} onChange={(e) => setFilter({ ...filter, kategorijaId: e.target.value })}>
            <option value="">Sve kategorije</option>
            {ravneKategorije(kategorije).map((k) => <option key={k.id} value={k.id}>{k.naziv}</option>)}
          </select>
          <IzborGrada vrednost={filter.gradId} promeni={(gradId) => setFilter({ ...filter, gradId })} praznaOpcija="Svi gradovi" />
          <button className="dugme-tamno"><Search className="h-4 w-4" /> Traži</button>
        </form>
      )}
      {r.ucitava ? <Ucitavanje /> : r.greska ? <Greska poruka={r.greska} ponovo={r.ponovo} /> : r.podaci?.stavke.length ? (
        <div className="grid gap-3 md:grid-cols-2">
          {r.podaci.stavke.map((x) => (
            <Link key={x.id} to={`/trazim/${x.id}`} className="kartica block p-4 transition hover:shadow-md">
              <div className="flex items-start justify-between gap-2">
                <h3 className="font-semibold">{x.naslov}</h3>
                <Znacka boja={x.status === 'Otvoren' ? 'bg-green-100 text-green-800' : 'bg-slate-100 text-slate-600'}>{x.status}</Znacka>
              </div>
              <p className="mt-1 line-clamp-2 text-sm text-slate-600">{x.opis}</p>
              <p className="mt-2 text-xs text-slate-500">
                {x.kategorijaNaziv}{x.gradNaziv && ` · ${x.gradNaziv}`}{x.maksimalnaCena && ` · do ${cena(x.maksimalnaCena)}`} · {x.korisnikIme} · {relativno(x.kreiranoUtc)} · {x.brojOdgovora} odgovora
              </p>
            </Link>
          ))}
          <div className="md:col-span-2"><Stranicenje stranica={r.podaci.stranica} ukupnoStranica={r.podaci.ukupnoStranica} promeni={setStranica} /></div>
        </div>
      ) : <Prazno naslov="Nema zahteva" />}

      <Modal naslov='Novi zahtev "Tražim..."' otvoren={novi} zatvori={() => setNovi(false)}>
        <form onSubmit={kreiraj} className="space-y-3" noValidate>
          <Polje oznaka="Šta tražite?" greska={greske.naslov}><input className="unos" maxLength={120} value={z.naslov} onChange={(e) => setZ({ ...z, naslov: e.target.value })} /></Polje>
          <Polje oznaka="Opis" greska={greske.opis}><textarea className="unos" rows={3} maxLength={2000} value={z.opis} onChange={(e) => setZ({ ...z, opis: e.target.value })} /></Polje>
          <Polje oznaka="Kategorija" greska={greske.kategorijaId}>
            <select className="unos" value={z.kategorijaId} onChange={(e) => setZ({ ...z, kategorijaId: e.target.value })}>
              <option value="">--</option>
              {ravneKategorije(kategorije).map((k) => <option key={k.id} value={k.id}>{k.naziv}</option>)}
            </select>
          </Polje>
          <div className="grid gap-3 sm:grid-cols-2">
            <Polje oznaka="Grad (opciono)">
              <IzborGrada vrednost={z.gradId} promeni={(gradId) => setZ({ ...z, gradId })} praznaOpcija="Bez grada" />
            </Polje>
            <Polje oznaka="Maksimalna cena (RSD)" greska={greske.maksimalnaCena}>
              <input className="unos" type="number" min={1} value={z.maksimalnaCena} onChange={(e) => setZ({ ...z, maksimalnaCena: e.target.value })} />
            </Polje>
          </div>
          <button className="dugme-primarno w-full">Objavi</button>
        </form>
      </Modal>
    </div>
  )
}
