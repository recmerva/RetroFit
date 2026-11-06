import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { ArrowLeft, Check, Handshake, Phone, Send, Undo2, X } from 'lucide-react'
import { porukaGreske } from '../api/klijent'
import { ponude, razgovori } from '../api/servisi'
import type { Poruka, Ponuda, RazgovorDetalji } from '../api/tipovi'
import { useAuth } from '../kontekst/AuthKontekst'
import { useToast } from '../kontekst/ToastKontekst'
import { SlikaIliZamena } from '../komponente/KarticaOglasa'
import { Greska, Modal, Prazno, StatusZnacka, Ucitavanje, Znacka } from '../komponente/Ui'
import { cena, iznos, nazivStatusaPonude, relativno, datumVreme } from '../pomocno/format'
import { useUpit } from '../pomocno/kuke'

function PanelPonuda({ r, osvezi }: { r: RazgovorDetalji; osvezi: () => void }) {
  const { korisnik } = useAuth()
  const toast = useToast()
  const navigacija = useNavigate()
  const [kontra, setKontra] = useState<Ponuda | null>(null)
  const [nova, setNova] = useState(false)
  const [vrednost, setVrednost] = useState('')
  const [radi, setRadi] = useState(false)
  const naCekanju = r.ponude.find((p) => p.status === 'NaCekanju')
  const mojRed = naCekanju && naCekanju.ponudioKorisnikId !== korisnik!.id

  const izvrsi = async (akcija: () => Promise<unknown>, poruka: string) => {
    setRadi(true)
    try {
      await akcija()
      toast.uspeh(poruka)
      setKontra(null)
      setNova(false)
      setVrednost('')
      osvezi()
    } catch (e) {
      toast.greska(porukaGreske(e))
    } finally {
      setRadi(false)
    }
  }

  return (
    <div className="space-y-2 border-b border-slate-200 bg-slate-50 p-3 text-sm">
      {naCekanju && (
        <div className="flex flex-wrap items-center justify-between gap-2 rounded-lg bg-white p-3 shadow-sm">
          <span>
            <Handshake className="mr-1 inline h-4 w-4 text-retro-600" />
            {naCekanju.ponudioKorisnikId === korisnik!.id ? 'Vaša ponuda' : 'Ponuda za vas'}: <strong>{iznos(naCekanju.iznos)}</strong>
            <span className="text-xs text-slate-500"> · ističe {relativno(naCekanju.isticeUtc)}</span>
            {naCekanju.napomena && <span className="block text-xs text-slate-500">"{naCekanju.napomena}"</span>}
          </span>
          {mojRed ? (
            <span className="flex gap-1">
              <button className="dugme-primarno px-3 py-1" disabled={radi}
                onClick={() => izvrsi(async () => { const rez = await ponude.prihvati(naCekanju.id); navigacija(`/rezervacije/${rez.id}`) }, 'Ponuda je prihvaćena - oglas je rezervisan.')}>
                <Check className="h-4 w-4" /> Prihvati
              </button>
              <button className="dugme-sekundarno px-3 py-1" onClick={() => { setKontra(naCekanju); setVrednost(String(naCekanju.iznos)) }}>
                <Undo2 className="h-4 w-4" /> Kontraponuda
              </button>
              <button className="dugme-sekundarno px-3 py-1 text-red-600" disabled={radi} onClick={() => izvrsi(() => ponude.odbij(naCekanju.id), 'Ponuda je odbijena.')}>
                <X className="h-4 w-4" /> Odbij
              </button>
            </span>
          ) : <Znacka boja="bg-amber-100 text-amber-800">Čeka odgovor</Znacka>}
        </div>
      )}
      <div className="flex flex-wrap gap-2">
        {r.jaSamKupac && r.oglas.status === 'Aktivan' && r.oglas.tip === 'Prodaja' && !naCekanju && (
          <button className="dugme-sekundarno px-3 py-1" onClick={() => setNova(true)}><Handshake className="h-4 w-4" /> Ponudi cenu</button>
        )}
        {!r.jaSamKupac && r.oglas.status === 'Aktivan' && (
          <button className="dugme-sekundarno px-3 py-1" disabled={radi}
            onClick={() => izvrsi(async () => { const rez = await razgovori.rezervisi(r.id); navigacija(`/rezervacije/${rez.id}`) }, 'Oglas je rezervisan za kupca.')}>
            Rezerviši za kupca ({cena(r.oglas.cena, r.oglas.tip)})
          </button>
        )}
        {r.aktivnaRezervacijaId && <Link to={`/rezervacije/${r.aktivnaRezervacijaId}`} className="dugme-tamno px-3 py-1">Rezervacija i kupovina →</Link>}
        {r.telefonProdavca && <a href={`tel:${r.telefonProdavca}`} className="dugme-sekundarno px-3 py-1"><Phone className="h-4 w-4" /> {r.telefonProdavca}</a>}
      </div>
      {r.ponude.length > 0 && (
        <details className="text-xs text-slate-600">
          <summary className="cursor-pointer">Istorija ponuda ({r.ponude.length})</summary>
          <ul className="mt-1 space-y-0.5">
            {r.ponude.map((p) => (
              <li key={p.id}>{datumVreme(p.kreiranoUtc)} · {p.ponudioKorisnikId === r.kupacId ? r.kupacIme : r.prodavacIme}: {iznos(p.iznos)} - {nazivStatusaPonude[p.status]}</li>
            ))}
          </ul>
        </details>
      )}
      <Modal naslov="Kontraponuda" otvoren={!!kontra} zatvori={() => setKontra(null)}>
        <input className="unos mb-3" type="number" min={1} value={vrednost} onChange={(e) => setVrednost(e.target.value)} />
        <button className="dugme-primarno w-full" disabled={radi || !(Number(vrednost) > 0)}
          onClick={() => kontra && izvrsi(() => ponude.kontraponuda(kontra.id, Number(vrednost)), 'Kontraponuda je poslata.')}>Pošalji kontraponudu</button>
      </Modal>
      <Modal naslov="Ponudi cenu" otvoren={nova} zatvori={() => setNova(false)}>
        <p className="mb-2 text-sm text-slate-600">Tražena cena: {cena(r.oglas.cena, r.oglas.tip)}</p>
        <input className="unos mb-3" type="number" min={1} value={vrednost} onChange={(e) => setVrednost(e.target.value)} />
        <button className="dugme-primarno w-full" disabled={radi || !(Number(vrednost) > 0)}
          onClick={() => izvrsi(() => ponude.daj(r.oglas.id, Number(vrednost)), 'Ponuda je poslata.')}>Pošalji ponudu</button>
      </Modal>
    </div>
  )
}

function Nit({ id }: { id: number }) {
  const { korisnik } = useAuth()
  const toast = useToast()
  const detalji = useUpit(() => razgovori.detalji(id), [id])
  const [poruke, setPoruke] = useState<Poruka[]>([])
  const [tekst, setTekst] = useState('')
  const brze = useUpit(() => razgovori.brzePoruke(), [])
  const dno = useRef<HTMLDivElement>(null)

  useEffect(() => {
    let aktivno = true
    const ucitaj = async () => {
      try {
        const s = await razgovori.poruke(id)
        if (!aktivno) return
        setPoruke([...s.stavke].reverse())
        await razgovori.procitano(id)
      } catch {
        /* greška se prikazuje kroz detalje */
      }
    }
    void ucitaj()
    const t = setInterval(ucitaj, 10_000)
    return () => {
      aktivno = false
      clearInterval(t)
    }
  }, [id])

  useEffect(() => dno.current?.scrollIntoView({ behavior: 'smooth' }), [poruke.length])

  const posalji = async (e?: FormEvent, brzaKljuc?: string) => {
    e?.preventDefault()
    if (!brzaKljuc && !tekst.trim()) return
    try {
      const p = await razgovori.posalji(id, brzaKljuc ? undefined : tekst.trim(), brzaKljuc)
      setPoruke((l) => [...l, p])
      setTekst('')
    } catch (err) {
      toast.greska(porukaGreske(err))
    }
  }

  if (detalji.ucitava && !detalji.podaci) return <Ucitavanje />
  if (detalji.greska || !detalji.podaci) return <Greska poruka={detalji.greska ?? 'Razgovor ne postoji.'} />
  const r = detalji.podaci

  return (
    <div className="flex h-[70vh] flex-col">
      <div className="flex items-center gap-3 border-b border-slate-200 p-3">
        <Link to="/poruke" className="lg:hidden" aria-label="Nazad"><ArrowLeft className="h-5 w-5" /></Link>
        <Link to={`/oglasi/${r.oglas.id}`} className="h-12 w-12 shrink-0 overflow-hidden rounded-lg">
          <SlikaIliZamena putanja={r.oglas.glavnaSlika} naslov={r.oglas.naslov} klasa="h-12" />
        </Link>
        <div className="min-w-0 flex-1">
          <Link to={`/oglasi/${r.oglas.id}`} className="block truncate font-semibold hover:underline">{r.oglas.naslov}</Link>
          <p className="text-xs text-slate-500">
            {cena(r.oglas.cena, r.oglas.tip)} · {r.jaSamKupac ? `Prodavac: ${r.prodavacIme}` : `Kupac: ${r.kupacIme}`}
          </p>
        </div>
        <StatusZnacka status={r.oglas.status} />
      </div>
      <PanelPonuda r={r} osvezi={detalji.ponovo} />
      <div className="flex-1 space-y-2 overflow-y-auto p-4">
        {poruke.map((p) => p.tip === 'Sistemska' ? (
          <p key={p.id} className="text-center text-xs text-slate-500"><span className="rounded-full bg-slate-100 px-3 py-1">{p.sadrzaj}</span></p>
        ) : (
          <div key={p.id} className={`flex ${p.posiljalacId === korisnik!.id ? 'justify-end' : 'justify-start'}`}>
            <div className={`max-w-[75%] rounded-2xl px-4 py-2 text-sm ${p.posiljalacId === korisnik!.id ? 'bg-retro-600 text-white' : 'bg-slate-100 text-slate-800'}`}>
              <p className="whitespace-pre-line">{p.sadrzaj}</p>
              <p className={`mt-1 text-[10px] ${p.posiljalacId === korisnik!.id ? 'text-retro-100' : 'text-slate-400'}`}>
                {relativno(p.poslatoUtc)}{p.posiljalacId === korisnik!.id && p.procitanoUtc ? ' · pročitano' : ''}
              </p>
            </div>
          </div>
        ))}
        <div ref={dno} />
      </div>
      <div className="border-t border-slate-200 p-3">
        <div className="mb-2 flex gap-1 overflow-x-auto">
          {brze.podaci?.map((b) => (
            <button key={b.kljuc} onClick={() => posalji(undefined, b.kljuc)} className="shrink-0 rounded-full bg-slate-100 px-3 py-1 text-xs hover:bg-retro-100">{b.tekst}</button>
          ))}
        </div>
        <form onSubmit={posalji} className="flex gap-2">
          <input className="unos" maxLength={2000} placeholder="Napišite poruku..." value={tekst} onChange={(e) => setTekst(e.target.value)} />
          <button className="dugme-primarno" aria-label="Pošalji"><Send className="h-4 w-4" /></button>
        </form>
      </div>
    </div>
  )
}

export default function Poruke() {
  const param = useParams().id
  const izabran = param ? Number(param) : null
  const lista = useUpit(() => razgovori.lista(), [izabran])

  return (
    <div className="kartica grid overflow-hidden lg:grid-cols-[320px_1fr]">
      <aside className={`border-r border-slate-200 ${izabran ? 'hidden lg:block' : ''}`}>
        <h1 className="border-b border-slate-200 p-4 text-lg font-bold">Poruke</h1>
        {lista.ucitava && !lista.podaci ? <Ucitavanje /> : lista.greska ? <Greska poruka={lista.greska} /> : lista.podaci?.stavke.length ? (
          <ul className="max-h-[70vh] overflow-y-auto">
            {lista.podaci.stavke.map((r) => (
              <li key={r.id}>
                <Link to={`/razgovori/${r.id}`} className={`flex gap-3 border-b border-slate-100 p-3 hover:bg-slate-50 ${r.id === izabran ? 'bg-retro-50' : ''}`}>
                  <span className="h-12 w-12 shrink-0 overflow-hidden rounded-lg"><SlikaIliZamena putanja={r.oglas.glavnaSlika} naslov={r.oglas.naslov} klasa="h-12" /></span>
                  <span className="min-w-0 flex-1">
                    <span className="flex justify-between gap-2">
                      <span className="truncate text-sm font-semibold">{r.sagovornikIme}</span>
                      <span className="shrink-0 text-[10px] text-slate-400">{relativno(r.poslednjaPorukaUtc)}</span>
                    </span>
                    <span className="block truncate text-xs text-slate-500">{r.oglas.naslov}</span>
                    <span className={`block truncate text-xs ${r.brojNeprocitanih ? 'font-semibold text-slate-800' : 'text-slate-400'}`}>{r.poslednjaPoruka}</span>
                  </span>
                  {r.brojNeprocitanih > 0 && <span className="h-5 min-w-5 rounded-full bg-retro-500 px-1 text-center text-[11px] leading-5 font-bold text-white">{r.brojNeprocitanih}</span>}
                </Link>
              </li>
            ))}
          </ul>
        ) : <div className="p-4"><Prazno naslov="Još nemate poruka" opis="Otvorite oglas i pošaljite poruku prodavcu." /></div>}
      </aside>
      <section className={izabran ? '' : 'hidden lg:block'}>
        {izabran ? <Nit key={izabran} id={izabran} /> : <div className="flex h-full items-center justify-center p-10 text-slate-400">Izaberite razgovor</div>}
      </section>
    </div>
  )
}
