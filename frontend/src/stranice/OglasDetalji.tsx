import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import {
  BadgeCheck, CalendarClock, Eye, Flag, Handshake, Heart, MapPin, MessageCircle, Pencil, Phone, RefreshCw, Rocket, ShieldX, Trash2, Truck, CheckCircle2, Send
} from 'lucide-react'
import { porukaGreske } from '../api/klijent'
import { moderacija, oglasi, omiljeni, ponude, razgovori } from '../api/servisi'
import type { RazlogPrijave } from '../api/tipovi'
import { useAuth } from '../kontekst/AuthKontekst'
import { useToast } from '../kontekst/ToastKontekst'
import { SlikaIliZamena } from '../komponente/KarticaOglasa'
import { Greska, Modal, StatusZnacka, Ucitavanje, Znacka, Zvezdice } from '../komponente/Ui'
import { cena, datum, iznos, nazivBedza, nazivRazloga, nazivStanja, relativno } from '../pomocno/format'
import { useUpit } from '../pomocno/kuke'

type Prozor = 'poruka' | 'ponuda' | 'prijava' | 'podizanje' | 'uklanjanje' | 'brisanje' | null

export default function OglasDetalji() {
  const id = Number(useParams().id)
  const { korisnik, imaUlogu } = useAuth()
  const toast = useToast()
  const navigacija = useNavigate()
  const { podaci: o, greska, ucitava, ponovo, setPodaci } = useUpit(() => oglasi.detalji(id), [id, korisnik?.id])
  const brze = useUpit(() => razgovori.brzePoruke(), [])
  const [slika, setSlika] = useState(0)
  const [prozor, setProzor] = useState<Prozor>(null)
  const [tekst, setTekst] = useState('')
  const [iznosPonude, setIznosPonude] = useState('')
  const [razlog, setRazlog] = useState<RazlogPrijave>('Prevara')
  const [dana, setDana] = useState(7)
  const [radi, setRadi] = useState(false)

  if (ucitava && !o) return <Ucitavanje />
  if (greska) return <Greska poruka={greska} ponovo={ponovo} />
  if (!o) return null

  const izvrsi = async (akcija: () => Promise<unknown>, poruka: string, posle?: () => void) => {
    setRadi(true)
    try {
      await akcija()
      toast.uspeh(poruka)
      setProzor(null)
      setTekst('')
      if (posle) posle()
      else await ponovo()
    } catch (e) {
      toast.greska(porukaGreske(e))
    } finally {
      setRadi(false)
    }
  }

  const traziPrijavu = () => navigacija('/prijava', { state: { nazad: `/oglasi/${id}` } })
  const jeModerator = imaUlogu('Moderator') || imaUlogu('Admin')
  const aktivan = o.status === 'Aktivan'
  const isticeUskoro = !!o.isticeUtc && new Date(o.isticeUtc).getTime() - Date.now() < 7 * 86400_000
  const trenutnaSlika = o.slike[slika]

  const prebaciOmiljeni = async () => {
    if (!korisnik) return traziPrijavu()
    const novo = !o.jeOmiljen
    setPodaci({ ...o, jeOmiljen: novo, brojOmiljenih: o.brojOmiljenih + (novo ? 1 : -1) })
    try {
      await (novo ? omiljeni.dodaj(o.id) : omiljeni.ukloni(o.id))
    } catch (e) {
      setPodaci(o)
      toast.greska(porukaGreske(e))
    }
  }

  return (
    <div className="grid gap-6 lg:grid-cols-[1fr_360px]">
      <div className="space-y-6">
        <div className="kartica overflow-hidden">
          <SlikaIliZamena putanja={trenutnaSlika?.putanja} naslov={o.naslov} klasa="h-80 md:h-[28rem] object-contain bg-slate-100" />
          {o.slike.length > 1 && (
            <div className="flex gap-2 overflow-x-auto p-3">
              {o.slike.map((s, i) => (
                <button key={s.id} onClick={() => setSlika(i)} className={`h-16 w-20 shrink-0 overflow-hidden rounded-lg border-2 ${i === slika ? 'border-retro-500' : 'border-transparent'}`}
                  aria-label={`Slika ${i + 1}`}>
                  <img src={s.putanja} alt="" className="h-full w-full object-cover" />
                </button>
              ))}
            </div>
          )}
        </div>

        <div className="kartica space-y-4 p-6">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <p className="text-sm text-slate-500">
                {o.nadredjenaKategorijaNaziv && <Link to={`/oglasi?kategorijaId=${o.nadredjenaKategorijaId}`} className="hover:underline">{o.nadredjenaKategorijaNaziv} › </Link>}
                <Link to={`/oglasi?kategorijaId=${o.kategorijaId}`} className="hover:underline">{o.kategorijaNaziv}</Link>
              </p>
              <h1 className="text-2xl font-bold">{o.naslov}</h1>
            </div>
            <div className="flex flex-wrap gap-2">
              <StatusZnacka status={o.status} />
              {o.jeIstaknut && <Znacka boja="bg-retro-100 text-retro-800">Istaknuto</Znacka>}
            </div>
          </div>
          <p className="text-3xl font-black text-retro-700">{cena(o.cena, o.tip)}</p>
          {o.zeljenaZamena && <p className="text-sm"><strong>Menja za:</strong> {o.zeljenaZamena}</p>}
          {o.status === 'Uklonjen' && o.razlogUklanjanja && (
            <p className="rounded-lg bg-red-50 p-3 text-sm text-red-700"><strong>Oglas je uklonjen:</strong> {o.razlogUklanjanja}</p>
          )}
          <div className="flex flex-wrap gap-4 text-sm text-slate-600">
            <span className="inline-flex items-center gap-1"><MapPin className="h-4 w-4" /> {o.gradNaziv}</span>
            <span>Stanje: <strong>{nazivStanja[o.stanje]}</strong></span>
            {o.dostavaMoguca && <span className="inline-flex items-center gap-1"><Truck className="h-4 w-4" /> Dostava</span>}
            {o.licnoPreuzimanjeMoguce && <span className="inline-flex items-center gap-1"><Handshake className="h-4 w-4" /> Lično preuzimanje</span>}
            <span className="inline-flex items-center gap-1"><Eye className="h-4 w-4" /> {o.brojPregleda}</span>
            <span className="inline-flex items-center gap-1"><Heart className="h-4 w-4" /> {o.brojOmiljenih}</span>
            <span className="inline-flex items-center gap-1"><CalendarClock className="h-4 w-4" /> Objavljen {relativno(o.objavljenoUtc ?? o.kreiranoUtc)}</span>
          </div>
          {o.atributi.length > 0 && (
            <dl className="grid gap-x-6 gap-y-2 rounded-lg bg-slate-50 p-4 text-sm sm:grid-cols-2">
              {o.atributi.map((a) => (
                <div key={a.definicijaAtributaId} className="flex justify-between gap-2 border-b border-slate-200 py-1">
                  <dt className="text-slate-500">{a.naziv}</dt>
                  <dd className="font-medium">{a.tip === 'DaNe' ? (a.vrednost === 'da' ? 'Da' : 'Ne') : a.vrednost} {a.jedinicaMere}</dd>
                </div>
              ))}
            </dl>
          )}
          <div>
            <h2 className="mb-2 font-semibold">Opis</h2>
            <p className="whitespace-pre-line text-slate-700">{o.opis}</p>
          </div>
        </div>
      </div>

      <aside className="space-y-4">
        <div className="kartica space-y-3 p-5">
          <Link to={`/korisnici/${o.prodavac.id}`} className="flex items-center gap-3">
            <span className="flex h-12 w-12 items-center justify-center rounded-full bg-noc-800 text-lg font-bold text-white">{o.prodavac.prikaznoIme[0]}</span>
            <span>
              <span className="flex items-center gap-1 font-semibold hover:underline">
                {o.prodavac.prikaznoIme}
                {o.prodavac.telefonVerifikovan && <BadgeCheck className="h-4 w-4 text-blue-600" />}
              </span>
              <span className="text-xs text-slate-500">Član od {datum(o.prodavac.clanOdUtc)} · {o.prodavac.gradNaziv}</span>
            </span>
          </Link>
          <div className="flex items-center justify-between text-sm">
            {o.prodavac.brojRecenzija > 0 ? <Zvezdice ocena={o.prodavac.prosecnaOcena} broj={o.prodavac.brojRecenzija} /> : <span className="text-slate-400">Još nema ocena</span>}
            <span className="text-slate-500">Reputacija <strong className="text-slate-800">{o.prodavac.reputacija}</strong>/100</span>
          </div>
          {o.prodavac.bedzevi.length > 0 && (
            <div className="flex flex-wrap gap-1">{o.prodavac.bedzevi.map((b) => <Znacka key={b} boja="bg-blue-50 text-blue-800">{nazivBedza[b] ?? b}</Znacka>)}</div>
          )}
          {o.telefonProdavca && (
            <a href={`tel:${o.telefonProdavca}`} className="dugme-tamno w-full"><Phone className="h-4 w-4" /> {o.telefonProdavca}</a>
          )}
          {!o.jeMoj && !o.telefonProdavca && korisnik && (
            <p className="text-xs text-slate-500">Broj telefona prodavca postaje vidljiv kada prodavac prihvati vašu ponudu.</p>
          )}
        </div>

        <div className="kartica space-y-2 p-5">
          {!korisnik && (
            <button className="dugme-primarno w-full" onClick={traziPrijavu}>Prijavite se da kontaktirate prodavca</button>
          )}
          {korisnik && !o.jeMoj && (
            <>
              {o.mojRazgovorId ? (
                <Link to={`/razgovori/${o.mojRazgovorId}`} className="dugme-primarno w-full"><MessageCircle className="h-4 w-4" /> Otvori razgovor</Link>
              ) : (
                aktivan && <button className="dugme-primarno w-full" onClick={() => setProzor('poruka')}><MessageCircle className="h-4 w-4" /> Pošalji poruku</button>
              )}
              {aktivan && o.tip === 'Prodaja' && (
                <button className="dugme-tamno w-full" onClick={() => setProzor('ponuda')}><Handshake className="h-4 w-4" /> Ponudi cenu</button>
              )}
              <button className="dugme-sekundarno w-full" onClick={prebaciOmiljeni}>
                <Heart className={`h-4 w-4 ${o.jeOmiljen ? 'fill-red-500 text-red-500' : ''}`} /> {o.jeOmiljen ? 'U omiljenima' : 'Dodaj u omiljene'}
              </button>
              <button className="w-full py-1 text-xs text-slate-500 hover:text-red-600" onClick={() => setProzor('prijava')}>
                <Flag className="mr-1 inline h-3 w-3" /> Prijavi sumnjiv oglas
              </button>
            </>
          )}
          {o.jeMoj && (
            <>
              <p className="text-xs text-slate-500">Ovo je vaš oglas{o.isticeUtc && o.status === 'Aktivan' ? ` · ističe ${relativno(o.isticeUtc)}` : ''}.</p>
              {['Nacrt', 'Aktivan', 'Istekao'].includes(o.status) && (
                <Link to={`/oglasi/${o.id}/izmena`} className="dugme-sekundarno w-full"><Pencil className="h-4 w-4" /> Izmeni oglas i slike</Link>
              )}
              {o.status === 'Nacrt' && (
                <button className="dugme-primarno w-full" disabled={radi} onClick={() => izvrsi(() => oglasi.objavi(o.id), 'Oglas je objavljen.')}>
                  <Send className="h-4 w-4" /> Objavi
                </button>
              )}
              {(o.status === 'Istekao' || (aktivan && isticeUskoro)) && (
                <button className="dugme-primarno w-full" disabled={radi} onClick={() => izvrsi(() => oglasi.obnovi(o.id), 'Oglas je obnovljen na 60 dana.')}>
                  <RefreshCw className="h-4 w-4" /> Obnovi oglas
                </button>
              )}
              {aktivan && (
                <>
                  <button className="dugme-tamno w-full" onClick={() => setProzor('podizanje')}><Rocket className="h-4 w-4" /> Podigni oglas</button>
                  <button className="dugme-sekundarno w-full" disabled={radi} onClick={() => izvrsi(() => oglasi.prodat(o.id), 'Oglas je označen kao prodat.')}>
                    <CheckCircle2 className="h-4 w-4" /> Označi kao prodato
                  </button>
                </>
              )}
              {o.status !== 'Rezervisan' && (
                <button className="dugme-sekundarno w-full text-red-600" onClick={() => setProzor('brisanje')}><Trash2 className="h-4 w-4" /> Obriši</button>
              )}
            </>
          )}
          {jeModerator && !o.jeMoj && o.status !== 'Uklonjen' && o.status !== 'Prodat' && (
            <button className="dugme-opasno w-full" onClick={() => setProzor('uklanjanje')}><ShieldX className="h-4 w-4" /> Ukloni oglas (moderator)</button>
          )}
        </div>
      </aside>

      <Modal naslov="Poruka prodavcu" otvoren={prozor === 'poruka'} zatvori={() => setProzor(null)}>
        <div className="mb-3 flex flex-wrap gap-2">
          {brze.podaci?.map((b) => (
            <button key={b.kljuc} className="rounded-full bg-slate-100 px-3 py-1 text-xs hover:bg-retro-100" disabled={radi}
              onClick={() => izvrsi(async () => navigacija(`/razgovori/${(await razgovori.zapocni(o.id, undefined, b.kljuc)).id}`), 'Poruka je poslata.', () => undefined)}>
              {b.tekst}
            </button>
          ))}
        </div>
        <textarea className="unos mb-3" rows={4} maxLength={2000} placeholder="Napišite poruku..." value={tekst} onChange={(e) => setTekst(e.target.value)} />
        <button className="dugme-primarno w-full" disabled={radi || !tekst.trim()}
          onClick={() => izvrsi(async () => navigacija(`/razgovori/${(await razgovori.zapocni(o.id, tekst.trim())).id}`), 'Poruka je poslata.', () => undefined)}>
          Pošalji
        </button>
      </Modal>

      <Modal naslov="Ponudi cenu" otvoren={prozor === 'ponuda'} zatvori={() => setProzor(null)}>
        <p className="mb-3 text-sm text-slate-600">Tražena cena: <strong>{cena(o.cena, o.tip)}</strong>. Ponuda važi 48 sati.</p>
        <input className="unos mb-3" type="number" min={1} placeholder="Vaša ponuda (RSD)" value={iznosPonude} onChange={(e) => setIznosPonude(e.target.value)} />
        <textarea className="unos mb-3" rows={2} maxLength={500} placeholder="Napomena (opciono)" value={tekst} onChange={(e) => setTekst(e.target.value)} />
        <button className="dugme-primarno w-full" disabled={radi || !(Number(iznosPonude) > 0)}
          onClick={() => izvrsi(() => ponude.daj(o.id, Number(iznosPonude), tekst.trim() || undefined), 'Ponuda je poslata prodavcu.')}>
          Pošalji ponudu
        </button>
      </Modal>

      <Modal naslov="Prijavi oglas" otvoren={prozor === 'prijava'} zatvori={() => setProzor(null)}>
        <select className="unos mb-3" value={razlog} onChange={(e) => setRazlog(e.target.value as RazlogPrijave)}>
          {(Object.keys(nazivRazloga) as RazlogPrijave[]).map((r) => <option key={r} value={r}>{nazivRazloga[r]}</option>)}
        </select>
        <textarea className="unos mb-3" rows={3} maxLength={1000} placeholder="Opišite problem (obavezno za 'Ostalo')" value={tekst} onChange={(e) => setTekst(e.target.value)} />
        <button className="dugme-opasno w-full" disabled={radi} onClick={() => izvrsi(() => oglasi.prijavi(o.id, razlog, tekst.trim() || undefined), 'Hvala! Moderator će pregledati prijavu.')}>
          Pošalji prijavu
        </button>
      </Modal>

      <Modal naslov="Podigni oglas na vrh" otvoren={prozor === 'podizanje'} zatvori={() => setProzor(null)}>
        <p className="mb-3 text-sm text-slate-600">Podignut oglas se prikazuje na vrhu pretrage i na početnoj strani. Cena: 99 RSD po danu (mock plaćanje).</p>
        <select className="unos mb-3" value={dana} onChange={(e) => setDana(Number(e.target.value))}>
          {[1, 3, 7, 14, 30].map((d) => <option key={d} value={d}>{d} {d === 1 ? 'dan' : 'dana'} - {iznos(d * 99)}</option>)}
        </select>
        <button className="dugme-primarno w-full" disabled={radi}
          onClick={() => izvrsi(async () => { const r = await oglasi.podigni(o.id, dana); toast.uspeh(`Plaćeno ${iznos(r.placeniIznos)} (ref. ${r.referencaPlacanja.slice(0, 18)}…)`) }, 'Oglas je podignut.')}>
          Plati i podigni
        </button>
      </Modal>

      <Modal naslov="Ukloni oglas" otvoren={prozor === 'uklanjanje'} zatvori={() => setProzor(null)}>
        <textarea className="unos mb-3" rows={3} placeholder="Razlog uklanjanja (vidi ga prodavac)" value={tekst} onChange={(e) => setTekst(e.target.value)} />
        <button className="dugme-opasno w-full" disabled={radi || !tekst.trim()} onClick={() => izvrsi(() => moderacija.ukloniOglas(o.id, tekst.trim()), 'Oglas je uklonjen.')}>
          Ukloni
        </button>
      </Modal>

      <Modal naslov="Obriši oglas?" otvoren={prozor === 'brisanje'} zatvori={() => setProzor(null)}>
        <p className="mb-4 text-sm text-slate-600">Oglas više neće biti vidljiv. Istorija prodaja i recenzija ostaje sačuvana.</p>
        <div className="flex gap-2">
          <button className="dugme-sekundarno flex-1" onClick={() => setProzor(null)}>Odustani</button>
          <button className="dugme-opasno flex-1" disabled={radi} onClick={() => izvrsi(() => oglasi.obrisi(o.id), 'Oglas je obrisan.', () => navigacija('/profil'))}>Obriši</button>
        </div>
      </Modal>
    </div>
  )
}
