import { useState } from 'react'
import { Link } from 'react-router'
import { Ban, Pencil, Plus, Power, Trash2 } from 'lucide-react'
import { porukaGreske } from '../api/klijent'
import { admin, katalog, moderacija } from '../api/servisi'
import type { Kategorija, KategorijaAudita, StavkaStatistike, TipAtributa, Uloga } from '../api/tipovi'
import { useToast } from '../kontekst/ToastKontekst'
import { Greska, Kartice, Modal, Naslov, Polje, Prazno, Stranicenje, Ucitavanje, Znacka } from '../komponente/Ui'
import { datumVreme, iznos } from '../pomocno/format'
import { osveziKesKategorija, useUpit } from '../pomocno/kuke'

type Kartica = 'statistika' | 'kategorije' | 'istaknuti' | 'uloge' | 'uredjaji' | 'audit'

// Grafikoni: jedna serija = jedna boja (validirana: kontrast >= 3:1 na beloj površini), bez legende.
const BOJA_SERIJE = '#ea580c'

function Plocica({ naziv, vrednost, opis }: { naziv: string; vrednost: string | number; opis?: string }) {
  return (
    <div className="kartica p-4">
      <p className="text-xs font-medium tracking-wide text-slate-500 uppercase">{naziv}</p>
      <p className="mt-1 text-2xl font-black text-slate-900">{vrednost}</p>
      {opis && <p className="text-xs text-slate-500">{opis}</p>}
    </div>
  )
}

/** Horizontalni stubići (rangiranje kategorija). Naziv i vrednost su tekst - čitljivo i bez boje. */
function HorizontalniStubici({ stavke }: { stavke: StavkaStatistike[] }) {
  const maks = Math.max(1, ...stavke.map((s) => s.vrednost))
  return (
    <ul className="space-y-2" aria-label="Aktivni oglasi po kategorijama">
      {stavke.map((s) => (
        <li key={s.oznaka} className="group grid grid-cols-[minmax(0,7rem)_1fr_2.5rem] sm:grid-cols-[150px_1fr_40px] items-center gap-3 text-sm" title={`${s.oznaka}: ${s.vrednost}`}>
          <span className="truncate text-slate-700">{s.oznaka}</span>
          <span className="h-3.5 rounded-sm bg-slate-100">
            <span className="block h-full rounded-r group-hover:opacity-80" style={{ width: `${(s.vrednost / maks) * 100}%`, background: BOJA_SERIJE }} />
          </span>
          <span className="text-right text-slate-600 tabular-nums">{s.vrednost}</span>
        </li>
      ))}
    </ul>
  )
}

/** Stubići kroz vreme (završene transakcije po danima) sa tooltip-om na prelaz mišem. */
function StubiciPoDanima({ stavke }: { stavke: StavkaStatistike[] }) {
  const [aktivan, setAktivan] = useState<number | null>(null)
  const maks = Math.max(1, ...stavke.map((s) => s.vrednost))
  const indeksMaks = stavke.findIndex((s) => s.vrednost === maks && maks > 0)
  return (
    <div>
      <div className="relative flex h-40 items-end gap-0.5 border-b border-slate-300" onMouseLeave={() => setAktivan(null)} role="img"
        aria-label="Završene transakcije po danima">
        {stavke.map((s, i) => (
          <div key={s.oznaka} className="relative flex h-full flex-1 items-end" onMouseEnter={() => setAktivan(i)}>
            <div className="w-full rounded-t" style={{ height: `${(s.vrednost / maks) * 100}%`, minHeight: s.vrednost ? 4 : 0, background: BOJA_SERIJE, opacity: aktivan === null || aktivan === i ? 1 : 0.5 }} />
            {(aktivan === i || (aktivan === null && i === indeksMaks)) && (
              <span className="absolute -top-7 left-1/2 -translate-x-1/2 rounded bg-slate-900 px-2 py-0.5 text-xs whitespace-nowrap text-white">
                {s.oznaka}: {s.vrednost}
              </span>
            )}
          </div>
        ))}
      </div>
      <div className="mt-1 flex justify-between text-[10px] text-slate-500">
        <span>{stavke[0]?.oznaka}</span><span>{stavke[stavke.length - 1]?.oznaka}</span>
      </div>
      <details className="mt-2 text-xs text-slate-600">
        <summary className="cursor-pointer">Prikaži kao tabelu</summary>
        <table className="mt-1"><tbody>{stavke.map((s) => <tr key={s.oznaka}><td className="pr-4">{s.oznaka}</td><td className="tabular-nums">{s.vrednost}</td></tr>)}</tbody></table>
      </details>
    </div>
  )
}

function Statistika() {
  const r = useUpit(() => admin.statistika(), [])
  if (r.ucitava) return <Ucitavanje />
  if (r.greska || !r.podaci) return <Greska poruka={r.greska ?? ''} ponovo={r.ponovo} />
  const s = r.podaci
  return (
    <div className="space-y-6">
      <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
        <Plocica naziv="Korisnici" vrednost={s.ukupnoKorisnika} opis={`+${s.noviKorisnici7Dana} za 7 dana`} />
        <Plocica naziv="Aktivni oglasi" vrednost={s.aktivniOglasi} opis={`+${s.noviOglasi7Dana} novih za 7 dana · ${s.rezervisaniOglasi} rezervisano`} />
        <Plocica naziv="Prodato" vrednost={s.prodatiOglasi} opis={`${s.zavrseneTransakcije} kroz platformu`} />
        <Plocica naziv="Promet" vrednost={iznos(s.ukupanPromet)} opis={`escrow ${iznos(s.prometEscrow)}`} />
        <Plocica naziv="Prihod od isticanja" vrednost={iznos(s.prihodOdIsticanja)} />
        <Plocica naziv="Prosečna ocena" vrednost={s.prosecnaOcenaPlatforme.toFixed(2)} opis="sve recenzije" />
        <Plocica naziv="Otvorene prijave" vrednost={s.otvorenePrijave} />
        <Plocica naziv="Bezbednost (24h)" vrednost={s.bezbednosniDogadjaji24h} opis={`${s.aktivneBlokadeKorisnika} blokiranih korisnika · ${s.aktivneBlokadeUredjaja} uređaja`} />
      </div>
      <div className="grid gap-6 lg:grid-cols-2">
        <div className="kartica p-5">
          <h2 className="mb-1 font-semibold">Aktivni oglasi po kategorijama</h2>
          <p className="mb-4 text-xs text-slate-500">Podkategorije su pripisane glavnoj kategoriji</p>
          {s.oglasiPoKategorijama.length ? <HorizontalniStubici stavke={s.oglasiPoKategorijama} /> : <Prazno naslov="Nema podataka" />}
        </div>
        <div className="kartica p-5">
          <h2 className="mb-1 font-semibold">Završene transakcije - poslednjih 14 dana</h2>
          <p className="mb-8 text-xs text-slate-500">Broj kupoprodaja po danu završetka</p>
          <StubiciPoDanima stavke={s.transakcijePoDanima} />
        </div>
      </div>
    </div>
  )
}

function Kategorije() {
  const toast = useToast()
  const r = useUpit(() => katalog.kategorije(true), [])
  const [izmena, setIzmena] = useState<{ id: number | null; naziv: string; roditeljId: string; redosled: string } | null>(null)
  const [atribut, setAtribut] = useState<{ kategorija: Kategorija; naziv: string; tip: TipAtributa; obavezan: boolean; opcije: string } | null>(null)

  const izvrsi = async (fn: () => Promise<unknown>, poruka: string) => {
    try {
      await fn()
      toast.uspeh(poruka)
      osveziKesKategorija()
      setIzmena(null)
      setAtribut(null)
      await r.ponovo()
    } catch (e) {
      toast.greska(porukaGreske(e))
    }
  }

  const red = (k: Kategorija, nivo: number) => (
    <div key={k.id}>
      <div className={`flex flex-wrap items-center gap-2 border-b border-slate-100 py-2 ${nivo ? 'pl-8' : ''}`}>
        <span className={`flex-1 ${nivo ? '' : 'font-semibold'} ${k.aktivna ? '' : 'text-slate-400 line-through'}`}>{k.naziv}</span>
        <span className="text-xs text-slate-500">{k.brojAktivnihOglasa} oglasa · /{k.slug}</span>
        <button className="dugme-sekundarno px-2 py-1" title="Izmeni"
          onClick={() => setIzmena({ id: k.id, naziv: k.naziv, roditeljId: k.roditeljId?.toString() ?? '', redosled: String(k.redosled) })}><Pencil className="h-3.5 w-3.5" /></button>
        {nivo > 0 && <button className="dugme-sekundarno px-2 py-1 text-xs" onClick={() => setAtribut({ kategorija: k, naziv: '', tip: 'Tekst', obavezan: false, opcije: '' })}>+ atribut</button>}
        <button className="dugme-sekundarno px-2 py-1" title={k.aktivna ? 'Deaktiviraj' : 'Aktiviraj'}
          onClick={() => izvrsi(() => katalog.aktivnostKategorije(k.id, !k.aktivna), k.aktivna ? 'Kategorija je deaktivirana.' : 'Kategorija je aktivirana.')}>
          <Power className={`h-3.5 w-3.5 ${k.aktivna ? 'text-green-600' : 'text-slate-400'}`} />
        </button>
        <button className="dugme-sekundarno px-2 py-1" title="Obriši" onClick={() => confirm(`Obrisati kategoriju "${k.naziv}"?`) && izvrsi(() => katalog.obrisiKategoriju(k.id), 'Kategorija je obrisana.')}>
          <Trash2 className="h-3.5 w-3.5 text-red-600" />
        </button>
      </div>
      {k.podkategorije.map((p) => red(p, nivo + 1))}
    </div>
  )

  if (r.ucitava) return <Ucitavanje />
  if (r.greska || !r.podaci) return <Greska poruka={r.greska ?? ''} />
  return (
    <div className="kartica p-4">
      <button className="dugme-primarno mb-3" onClick={() => setIzmena({ id: null, naziv: '', roditeljId: '', redosled: '0' })}><Plus className="h-4 w-4" /> Nova kategorija</button>
      {r.podaci.map((k) => red(k, 0))}
      <Modal naslov={izmena?.id ? 'Izmena kategorije' : 'Nova kategorija'} otvoren={!!izmena} zatvori={() => setIzmena(null)}>
        {izmena && (
          <div className="space-y-3">
            <Polje oznaka="Naziv"><input className="unos" value={izmena.naziv} onChange={(e) => setIzmena({ ...izmena, naziv: e.target.value })} /></Polje>
            <Polje oznaka="Nadređena kategorija">
              <select className="unos" value={izmena.roditeljId} onChange={(e) => setIzmena({ ...izmena, roditeljId: e.target.value })}>
                <option value="">(glavna kategorija)</option>
                {r.podaci!.filter((k) => k.id !== izmena.id).map((k) => <option key={k.id} value={k.id}>{k.naziv}</option>)}
              </select>
            </Polje>
            <Polje oznaka="Redosled"><input className="unos" type="number" value={izmena.redosled} onChange={(e) => setIzmena({ ...izmena, redosled: e.target.value })} /></Polje>
            <button className="dugme-primarno w-full" disabled={!izmena.naziv.trim()}
              onClick={() => izvrsi(() => katalog.sacuvajKategoriju(izmena.id, { naziv: izmena.naziv.trim(), roditeljId: izmena.roditeljId ? Number(izmena.roditeljId) : undefined, redosled: Number(izmena.redosled) || 0 }), 'Kategorija je sačuvana.')}>
              Sačuvaj
            </button>
          </div>
        )}
      </Modal>
      <Modal naslov={`Novi atribut: ${atribut?.kategorija.naziv ?? ''}`} otvoren={!!atribut} zatvori={() => setAtribut(null)}>
        {atribut && (
          <div className="space-y-3">
            <Polje oznaka="Naziv"><input className="unos" value={atribut.naziv} onChange={(e) => setAtribut({ ...atribut, naziv: e.target.value })} /></Polje>
            <Polje oznaka="Tip">
              <select className="unos" value={atribut.tip} onChange={(e) => setAtribut({ ...atribut, tip: e.target.value as TipAtributa })}>
                <option value="Tekst">Tekst</option><option value="Broj">Broj</option><option value="DaNe">Da/Ne</option><option value="Izbor">Izbor iz liste</option>
              </select>
            </Polje>
            {atribut.tip === 'Izbor' && <Polje oznaka="Opcije (razdvojene zarezom)"><input className="unos" value={atribut.opcije} onChange={(e) => setAtribut({ ...atribut, opcije: e.target.value })} /></Polje>}
            <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={atribut.obavezan} onChange={(e) => setAtribut({ ...atribut, obavezan: e.target.checked })} /> Obavezan</label>
            <button className="dugme-primarno w-full" disabled={!atribut.naziv.trim()}
              onClick={() => izvrsi(() => katalog.dodajAtribut(atribut.kategorija.id, {
                naziv: atribut.naziv.trim(), tip: atribut.tip, obavezan: atribut.obavezan, redosled: 99,
                opcije: atribut.tip === 'Izbor' ? atribut.opcije.split(',').map((o) => o.trim()).filter(Boolean) : undefined
              }), 'Atribut je dodat.')}>Dodaj</button>
          </div>
        )}
      </Modal>
    </div>
  )
}

function Istaknuti() {
  const toast = useToast()
  const r = useUpit(() => admin.istaknuti(), [])
  const [oglasId, setOglasId] = useState('')
  const [dana, setDana] = useState('7')
  const izvrsi = async (fn: () => Promise<unknown>, poruka: string) => {
    try {
      await fn()
      toast.uspeh(poruka)
      await r.ponovo()
    } catch (e) {
      toast.greska(porukaGreske(e))
    }
  }
  return (
    <div className="space-y-4">
      <div className="kartica flex flex-wrap items-end gap-2 p-4">
        <Polje oznaka="ID oglasa"><input className="unos w-32" type="number" value={oglasId} onChange={(e) => setOglasId(e.target.value)} /></Polje>
        <Polje oznaka="Broj dana"><input className="unos w-24" type="number" min={1} max={90} value={dana} onChange={(e) => setDana(e.target.value)} /></Polje>
        <button className="dugme-primarno" disabled={!oglasId} onClick={() => izvrsi(() => admin.izdvoji(Number(oglasId), Number(dana)), 'Oglas je izdvojen na početnoj.')}>Izdvoji oglas</button>
      </div>
      {r.ucitava ? <Ucitavanje /> : r.podaci?.stavke.length ? (
        <div className="kartica divide-y divide-slate-100">
          {r.podaci.stavke.map((i) => (
            <div key={i.id} className="flex flex-wrap items-center gap-3 p-3 text-sm">
              <Link to={`/oglasi/${i.oglasId}`} className="flex-1 font-medium hover:underline">#{i.oglasId} {i.oglasNaslov}</Link>
              <Znacka boja={i.tip === 'Podignut' ? 'bg-retro-100 text-retro-800' : 'bg-blue-100 text-blue-800'}>{i.tip === 'Podignut' ? `Plaćeno ${iznos(i.placeniIznos ?? 0)}` : 'Izdvojio admin'}</Znacka>
              <span className="text-xs text-slate-500">do {datumVreme(i.krajUtc)} · {i.kreiraoIme}</span>
              {!i.otkazanoUtc && <button className="dugme-sekundarno px-2 py-1" onClick={() => izvrsi(() => admin.otkaziIsticanje(i.id), 'Isticanje je otkazano.')}>Otkaži</button>}
            </div>
          ))}
        </div>
      ) : <Prazno naslov="Nema aktivnih isticanja" />}
    </div>
  )
}

function Uloge() {
  const toast = useToast()
  const [upit, setUpit] = useState('')
  const [primenjen, setPrimenjen] = useState('')
  const r = useUpit(() => moderacija.korisnici(primenjen || undefined), [primenjen])
  const promeni = async (id: number, uloge: Uloga[], uloga: Uloga, ukljuci: boolean) => {
    const nove = ukljuci ? [...new Set([...uloge, uloga])] : uloge.filter((u) => u !== uloga)
    try {
      await admin.uloge(id, nove)
      toast.uspeh('Uloge su promenjene. Korisnik mora ponovo da se prijavi.')
      await r.ponovo()
    } catch (e) {
      toast.greska(porukaGreske(e))
    }
  }
  return (
    <div>
      <form className="mb-4 flex gap-2" onSubmit={(e) => { e.preventDefault(); setPrimenjen(upit) }}>
        <input className="unos" placeholder="Pretraga korisnika" value={upit} onChange={(e) => setUpit(e.target.value)} /><button className="dugme-tamno">Traži</button>
      </form>
      {r.ucitava ? <Ucitavanje /> : (
        <div className="kartica overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead className="bg-slate-50 text-xs text-slate-500"><tr><th className="p-3">Korisnik</th><th>Moderator</th><th>Admin</th></tr></thead>
            <tbody>
              {r.podaci?.stavke.map((k) => (
                <tr key={k.id} className="border-t border-slate-100">
                  <td className="p-3">{k.imePrezime}<span className="block text-xs text-slate-500">{k.email}</span></td>
                  {(['Moderator', 'Admin'] as Uloga[]).map((u) => (
                    <td key={u}><input type="checkbox" className="h-4 w-4 accent-retro-600" checked={k.uloge.includes(u)} disabled={k.obrisan}
                      onChange={(e) => promeni(k.id, k.uloge, u, e.target.checked)} aria-label={`${u}: ${k.imePrezime}`} /></td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

function Uredjaji() {
  const toast = useToast()
  const [minRizik, setMinRizik] = useState(30)
  const r = useUpit(() => admin.uredjaji(minRizik), [minRizik])
  const blokade = useUpit(() => admin.blokadeUredjaja(), [])
  const izvrsi = async (fn: () => Promise<unknown>, poruka: string) => {
    try {
      await fn()
      toast.uspeh(poruka)
      await Promise.all([r.ponovo(), blokade.ponovo()])
    } catch (e) {
      toast.greska(porukaGreske(e))
    }
  }
  return (
    <div className="space-y-6">
      <div className="rounded-lg bg-blue-50 p-4 text-sm text-blue-900">
        Otisak uređaja je <strong>HMAC heš grubih signala</strong> (pregledač, jezik, ekran, vremenska zona) - ne sadrži lične podatke i nije pouzdan
        jedinstveni identifikator. Blokada uređaja je dopuna blokadi naloga, ne zamena.
      </div>
      <div className="flex items-center gap-2 text-sm">
        Minimalni rizik:
        <select className="unos w-auto" value={minRizik} onChange={(e) => setMinRizik(Number(e.target.value))}>
          {[0, 30, 50, 85].map((v) => <option key={v} value={v}>{v}</option>)}
        </select>
      </div>
      {r.ucitava ? <Ucitavanje /> : r.podaci?.stavke.length ? (
        <div className="kartica overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead className="bg-slate-50 text-xs text-slate-500"><tr><th className="p-3">Otisak</th><th>Klijent</th><th>Zahteva</th><th>Registracija</th><th>Max rizik</th><th>Poslednji put</th><th /></tr></thead>
            <tbody>
              {r.podaci.stavke.map((o) => (
                <tr key={o.id} className="border-t border-slate-100">
                  <td className="p-3 font-mono text-xs">{o.otisakSkraceno}…</td>
                  <td>{o.opisKlijenta ?? '-'}</td>
                  <td className="tabular-nums">{o.brojZahteva}</td>
                  <td className="tabular-nums">{o.brojRegistracija}</td>
                  <td><Znacka boja={o.najveciRizik >= 85 ? 'bg-red-100 text-red-800' : o.najveciRizik >= 50 ? 'bg-amber-100 text-amber-800' : 'bg-slate-100 text-slate-700'}>{o.najveciRizik}</Znacka></td>
                  <td className="text-xs">{datumVreme(o.poslednjiPutVidjenUtc)}</td>
                  <td className="pr-3 text-right">
                    {o.blokiran ? <Znacka boja="bg-red-100 text-red-800">Blokiran</Znacka> : (
                      <button className="dugme-sekundarno px-2 py-1 text-red-600"
                        onClick={() => izvrsi(() => admin.blokirajUredjaj(o.id, prompt('Razlog blokade:') || 'Sumnjiva aktivnost', 7), 'Uređaj je blokiran na 7 dana.')}>
                        <Ban className="h-4 w-4" /> Blokiraj
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : <Prazno naslov="Nema uređaja sa ovim nivoom rizika" />}
      <div>
        <h2 className="mb-2 font-semibold">Aktivne blokade uređaja</h2>
        {blokade.podaci?.stavke.length ? (
          <div className="kartica divide-y divide-slate-100">
            {blokade.podaci.stavke.map((b) => (
              <div key={b.id} className="flex flex-wrap items-center gap-3 p-3 text-sm">
                <span className="font-mono text-xs">{b.otisakSkraceno}…</span>
                <span className="flex-1">{b.razlog}</span>
                <Znacka>{b.automatska ? 'Automatska' : 'Ručna'}</Znacka>
                <span className="text-xs text-slate-500">{b.krajUtc ? `do ${datumVreme(b.krajUtc)}` : 'trajno'}</span>
                <button className="dugme-sekundarno px-2 py-1" onClick={() => izvrsi(() => admin.ukiniBlokaduUredjaja(b.id), 'Blokada je ukinuta.')}>Ukini</button>
              </div>
            ))}
          </div>
        ) : <p className="text-sm text-slate-500">Nema aktivnih blokada.</p>}
      </div>
    </div>
  )
}

function Audit() {
  const [kategorija, setKategorija] = useState<KategorijaAudita | ''>('')
  const [stranica, setStranica] = useState(1)
  const r = useUpit(() => admin.audit({ kategorija: kategorija || undefined, stranica }), [kategorija, stranica])
  return (
    <div>
      <select className="unos mb-4 w-auto" value={kategorija} onChange={(e) => { setKategorija(e.target.value as KategorijaAudita | ''); setStranica(1) }}>
        <option value="">Sve kategorije</option>
        {(['Podaci', 'Bezbednost', 'Moderacija', 'Administracija'] as KategorijaAudita[]).map((k) => <option key={k} value={k}>{k}</option>)}
      </select>
      {r.ucitava ? <Ucitavanje /> : r.greska ? <Greska poruka={r.greska} /> : r.podaci?.stavke.length ? (
        <div className="kartica overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className="bg-slate-50 text-slate-500"><tr><th className="p-2">Vreme</th><th>Kategorija</th><th>Akcija</th><th>Entitet</th><th>Korisnik</th><th>IP</th><th>Detalji</th></tr></thead>
            <tbody>
              {r.podaci.stavke.map((a) => (
                <tr key={a.id} className="border-t border-slate-100 align-top">
                  <td className="p-2 whitespace-nowrap">{datumVreme(a.kreiranoUtc)}</td>
                  <td><Znacka boja={a.kategorija === 'Bezbednost' ? 'bg-red-50 text-red-800' : 'bg-slate-100 text-slate-700'}>{a.kategorija}</Znacka></td>
                  <td className="font-medium">{a.akcija}</td>
                  <td>{a.nazivEntiteta}{a.entitetId && ` #${a.entitetId}`}</td>
                  <td>{a.korisnikEmail ?? '-'}</td>
                  <td className="font-mono">{a.ipAdresa ?? '-'}</td>
                  <td className="max-w-md">
                    {(a.stareVrednosti || a.noveVrednosti) && (
                      <details><summary className="cursor-pointer text-retro-700">JSON</summary>
                        {a.stareVrednosti && <pre className="mt-1 whitespace-pre-wrap break-all text-red-700">- {a.stareVrednosti}</pre>}
                        {a.noveVrednosti && <pre className="mt-1 whitespace-pre-wrap break-all text-green-700">+ {a.noveVrednosti}</pre>}
                      </details>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <Stranicenje stranica={r.podaci.stranica} ukupnoStranica={r.podaci.ukupnoStranica} promeni={setStranica} />
        </div>
      ) : <Prazno naslov="Nema zapisa" />}
    </div>
  )
}

export default function AdminPanel() {
  const [kartica, setKartica] = useState<Kartica>('statistika')
  return (
    <div>
      <Naslov>Administracija</Naslov>
      <Kartice aktivna={kartica} promeni={setKartica} kartice={[
        { kljuc: 'statistika', naziv: 'Statistika' }, { kljuc: 'kategorije', naziv: 'Kategorije' }, { kljuc: 'istaknuti', naziv: 'Izdvojeni oglasi' },
        { kljuc: 'uloge', naziv: 'Uloge' }, { kljuc: 'uredjaji', naziv: 'Uređaji (bot zaštita)' }, { kljuc: 'audit', naziv: 'Audit log' }
      ]} />
      {kartica === 'statistika' && <Statistika />}
      {kartica === 'kategorije' && <Kategorije />}
      {kartica === 'istaknuti' && <Istaknuti />}
      {kartica === 'uloge' && <Uloge />}
      {kartica === 'uredjaji' && <Uredjaji />}
      {kartica === 'audit' && <Audit />}
    </div>
  )
}
