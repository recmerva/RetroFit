import { useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router'
import { CheckCircle2, Circle, CreditCard, Lock, Phone, Star } from 'lucide-react'
import { porukaGreske } from '../api/klijent'
import { trgovina } from '../api/servisi'
import type { NacinPreuzimanja, Rezervacija, Transakcija } from '../api/tipovi'
import { useAuth } from '../kontekst/AuthKontekst'
import { useToast } from '../kontekst/ToastKontekst'
import { SlikaIliZamena } from '../komponente/KarticaOglasa'
import { BiracOcene, Greska, Kartice, Modal, Naslov, Prazno, Ucitavanje, Znacka } from '../komponente/Ui'
import { cena, datumVreme, iznos, nazivEscrowa, nazivStatusaRezervacije, nazivStatusaTransakcije, relativno } from '../pomocno/format'
import { useUpit } from '../pomocno/kuke'

function Korak({ gotov, tekst, vreme }: { gotov: boolean; tekst: string; vreme?: string }) {
  return (
    <li className="flex items-center gap-2 text-sm">
      {gotov ? <CheckCircle2 className="h-4 w-4 text-green-600" /> : <Circle className="h-4 w-4 text-slate-300" />}
      <span className={gotov ? 'text-slate-800' : 'text-slate-400'}>{tekst}</span>
      {vreme && <span className="text-xs text-slate-400">{datumVreme(vreme)}</span>}
    </li>
  )
}

function KarticaRezervacije({ r, osvezi }: { r: Rezervacija; osvezi: () => void }) {
  const { korisnik } = useAuth()
  const toast = useToast()
  const navigacija = useNavigate()
  const [kupovina, setKupovina] = useState(false)
  const [nacin, setNacin] = useState<NacinPreuzimanja>('LicnoPreuzimanje')
  const [adresa, setAdresa] = useState('')
  const [escrow, setEscrow] = useState(true)
  const jaSamKupac = r.kupacId === korisnik!.id
  const aktivna = r.status === 'Aktivna'

  const zapocni = async () => {
    try {
      const t = await trgovina.zapocniKupovinu(r.id, nacin, nacin === 'Dostava' ? adresa : undefined, escrow && r.dogovorenaCena !== undefined && r.dogovorenaCena !== null)
      toast.uspeh('Kupovina je pokrenuta.')
      navigacija(`/transakcije/${t.id}`)
    } catch (e) {
      toast.greska(porukaGreske(e))
    }
  }

  const otkazi = async () => {
    const razlog = prompt('Razlog otkazivanja (opciono):') ?? undefined
    try {
      await trgovina.otkaziRezervaciju(r.id, razlog || undefined)
      toast.uspeh('Rezervacija je otkazana. Oglas je ponovo aktivan.')
      osvezi()
    } catch (e) {
      toast.greska(porukaGreske(e))
    }
  }

  return (
    <div className="kartica flex flex-wrap items-center gap-4 p-4">
      <Link to={`/oglasi/${r.oglas.id}`} className="h-16 w-16 shrink-0 overflow-hidden rounded-lg"><SlikaIliZamena putanja={r.oglas.glavnaSlika} naslov={r.oglas.naslov} klasa="h-16" /></Link>
      <div className="min-w-0 flex-1">
        <Link to={`/oglasi/${r.oglas.id}`} className="font-semibold hover:underline">{r.oglas.naslov}</Link>
        <p className="text-sm text-slate-600">
          {jaSamKupac ? `Prodavac: ${r.prodavacIme}` : `Kupac: ${r.kupacIme}`} · dogovoreno <strong>{cena(r.dogovorenaCena, r.oglas.tip)}</strong>
        </p>
        <p className="text-xs text-slate-500">{aktivna ? `Ističe ${relativno(r.isticeUtc)} (${datumVreme(r.isticeUtc)})` : r.razlogOtkazivanja ?? relativno(r.zavrsenoUtc)}</p>
      </div>
      <Znacka boja={aktivna ? 'bg-amber-100 text-amber-800' : 'bg-slate-100 text-slate-700'}>{nazivStatusaRezervacije[r.status]}</Znacka>
      <div className="flex gap-2">
        {aktivna && jaSamKupac && <button className="dugme-primarno px-3 py-1" onClick={() => setKupovina(true)}>Pokreni kupovinu</button>}
        {aktivna && <button className="dugme-sekundarno px-3 py-1" onClick={otkazi}>Otkaži</button>}
        {r.transakcijaId && <Link className="dugme-tamno px-3 py-1" to={`/transakcije/${r.transakcijaId}`}>Transakcija</Link>}
      </div>
      <Modal naslov="Pokretanje kupovine" otvoren={kupovina} zatvori={() => setKupovina(false)}>
        <div className="space-y-3">
          <div className="flex gap-2">
            {(['LicnoPreuzimanje', 'Dostava'] as NacinPreuzimanja[]).map((n) => (
              <button key={n} type="button" onClick={() => setNacin(n)} className={nacin === n ? 'dugme-primarno flex-1' : 'dugme-sekundarno flex-1'}>
                {n === 'Dostava' ? 'Dostava' : 'Lično preuzimanje'}
              </button>
            ))}
          </div>
          {nacin === 'Dostava' && <input className="unos" placeholder="Adresa za dostavu (vidi je samo prodavac)" value={adresa} onChange={(e) => setAdresa(e.target.value)} />}
          {r.dogovorenaCena ? (
            <label className="flex items-start gap-2 rounded-lg bg-green-50 p-3 text-sm">
              <input type="checkbox" className="mt-1 accent-green-700" checked={escrow} onChange={(e) => setEscrow(e.target.checked)} />
              <span><Lock className="mr-1 inline h-4 w-4" /><strong>Sigurna kupovina (escrow)</strong>: novac se drži dok ne potvrdite da ste primili predmet. Mock plaćanje - bez prave kartice.</span>
            </label>
          ) : null}
          <button className="dugme-primarno w-full" disabled={nacin === 'Dostava' && !adresa.trim()} onClick={zapocni}>Potvrdi</button>
        </div>
      </Modal>
    </div>
  )
}

function DetaljiTransakcije({ id }: { id: number }) {
  const { korisnik } = useAuth()
  const toast = useToast()
  const r = useUpit(() => trgovina.transakcija(id), [id])
  const [recenzija, setRecenzija] = useState(false)
  const [ocena, setOcena] = useState(5)
  const [komentar, setKomentar] = useState('')
  const [radi, setRadi] = useState(false)

  if (r.ucitava && !r.podaci) return <Ucitavanje />
  if (r.greska || !r.podaci) return <Greska poruka={r.greska ?? 'Transakcija ne postoji.'} />
  const t: Transakcija = r.podaci
  const jaSamKupac = t.kupacId === korisnik!.id
  const uToku = t.status === 'UToku'
  const mojaPotvrda = jaSamKupac ? t.kupacPotvrdioUtc : t.prodavacPotvrdioUtc
  const ostavioRecenziju = jaSamKupac ? t.kupacOstavioRecenziju : t.prodavacOstavioRecenziju

  const izvrsi = async (fn: () => Promise<Transakcija | unknown>, poruka: string) => {
    setRadi(true)
    try {
      await fn()
      toast.uspeh(poruka)
      setRecenzija(false)
      await r.ponovo()
    } catch (e) {
      toast.greska(porukaGreske(e))
    } finally {
      setRadi(false)
    }
  }

  return (
    <div className="kartica mb-6 grid gap-6 p-6 md:grid-cols-[1fr_300px]">
      <div>
        <div className="mb-3 flex flex-wrap items-center gap-2">
          <h2 className="text-xl font-bold">{t.oglas.naslov}</h2>
          <Znacka boja={t.status === 'Zavrsena' ? 'bg-green-100 text-green-800' : t.status === 'Otkazana' ? 'bg-red-100 text-red-800' : 'bg-amber-100 text-amber-800'}>
            {nazivStatusaTransakcije[t.status]}
          </Znacka>
        </div>
        <p className="text-sm text-slate-600">
          {jaSamKupac ? `Kupujete od: ${t.prodavacIme}` : `Prodajete kupcu: ${t.kupacIme}`} · {cena(t.cena, t.oglas.tip)} ·{' '}
          {t.nacinPreuzimanja === 'Dostava' ? `dostava na: ${t.adresaDostave}` : 'lično preuzimanje'}
        </p>
        {t.telefonDrugeStrane && <a href={`tel:${t.telefonDrugeStrane}`} className="mt-2 inline-flex items-center gap-1 text-sm font-semibold text-retro-700"><Phone className="h-4 w-4" /> {t.telefonDrugeStrane}</a>}
        <ol className="mt-4 space-y-1">
          <Korak gotov tekst="Kupovina pokrenuta" vreme={t.kreiranoUtc} />
          {t.statusEscrowa !== 'NijeKoriscen' && <Korak gotov={t.statusEscrowa !== 'CekaUplatu'} tekst={`Escrow: ${nazivEscrowa[t.statusEscrowa]}`} />}
          <Korak gotov={!!t.prodavacPotvrdioUtc} tekst="Prodavac potvrdio predaju/slanje" vreme={t.prodavacPotvrdioUtc} />
          <Korak gotov={!!t.kupacPotvrdioUtc} tekst="Kupac potvrdio prijem" vreme={t.kupacPotvrdioUtc} />
          <Korak gotov={t.status === 'Zavrsena'} tekst="Kupovina završena" vreme={t.zavrsenoUtc} />
        </ol>
        {t.razlogOtkazivanja && <p className="mt-2 text-sm text-red-700">Otkazano: {t.razlogOtkazivanja}</p>}
        {t.placanja.length > 0 && (
          <table className="mt-4 w-full text-left text-xs">
            <thead className="text-slate-500"><tr><th>Plaćanje</th><th>Iznos</th><th>Referenca</th><th>Vreme</th></tr></thead>
            <tbody>
              {t.placanja.map((p) => (
                <tr key={p.id} className="border-t border-slate-100"><td>{p.tip}</td><td>{iznos(p.iznos)}</td><td className="font-mono">{p.referencaProcesora.slice(0, 22)}…</td><td>{datumVreme(p.kreiranoUtc)}</td></tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
      <div className="space-y-2">
        {uToku && jaSamKupac && t.statusEscrowa === 'CekaUplatu' && (
          <button className="dugme-primarno w-full" disabled={radi} onClick={() => izvrsi(() => trgovina.uplati(t.id), 'Novac je deponovan na escrow.')}>
            <CreditCard className="h-4 w-4" /> Uplati {cena(t.cena)} (mock)
          </button>
        )}
        {uToku && !mojaPotvrda && t.statusEscrowa !== 'CekaUplatu' && (
          <button className="dugme-tamno w-full" disabled={radi} onClick={() => izvrsi(() => trgovina.potvrdi(t.id), 'Potvrđeno.')}>
            {jaSamKupac ? 'Potvrđujem da sam primio/la predmet' : 'Potvrđujem predaju/slanje'}
          </button>
        )}
        {uToku && mojaPotvrda && <p className="text-sm text-slate-500">Čeka se potvrda druge strane.</p>}
        {uToku && !(t.kupacPotvrdioUtc && t.prodavacPotvrdioUtc) && (
          <button className="dugme-sekundarno w-full" disabled={radi}
            onClick={() => izvrsi(() => trgovina.otkazi(t.id, prompt('Razlog otkazivanja (opciono):') || undefined), 'Kupovina je otkazana.')}>
            Otkaži kupovinu
          </button>
        )}
        {t.status === 'Zavrsena' && !ostavioRecenziju && (
          <button className="dugme-primarno w-full" onClick={() => setRecenzija(true)}><Star className="h-4 w-4" /> Oceni {jaSamKupac ? 'prodavca' : 'kupca'}</button>
        )}
        {t.status === 'Zavrsena' && ostavioRecenziju && <p className="text-sm text-green-700">Hvala, ostavili ste recenziju.</p>}
      </div>
      <Modal naslov="Recenzija" otvoren={recenzija} zatvori={() => setRecenzija(false)}>
        <div className="space-y-3">
          <BiracOcene vrednost={ocena} promeni={setOcena} />
          <textarea className="unos" rows={3} maxLength={1000} placeholder="Kako je prošla kupoprodaja?" value={komentar} onChange={(e) => setKomentar(e.target.value)} />
          <button className="dugme-primarno w-full" disabled={radi} onClick={() => izvrsi(() => trgovina.recenzija(t.id, ocena, komentar.trim() || undefined), 'Recenzija je sačuvana.')}>
            Pošalji recenziju
          </button>
        </div>
      </Modal>
    </div>
  )
}

export default function Transakcije() {
  const { korisnik } = useAuth()
  const lokacija = useLocation()
  const parametar = useParams().id
  const jeRezervacija = lokacija.pathname.startsWith('/rezervacije')
  const [kartica, setKartica] = useState<'rezervacije' | 'transakcije'>(jeRezervacija ? 'rezervacije' : 'transakcije')
  const rezervacije = useUpit(() => trgovina.rezervacije(), [lokacija.pathname])
  const transakcije = useUpit(() => trgovina.transakcije(), [lokacija.pathname])

  return (
    <div>
      <Naslov>Kupovine i prodaje</Naslov>
      {parametar && !jeRezervacija && <DetaljiTransakcije id={Number(parametar)} />}
      <Kartice aktivna={kartica} promeni={setKartica} kartice={[
        { kljuc: 'rezervacije', naziv: 'Rezervacije', broj: rezervacije.podaci?.stavke.filter((r) => r.status === 'Aktivna').length },
        { kljuc: 'transakcije', naziv: 'Transakcije', broj: transakcije.podaci?.stavke.filter((t) => t.status === 'UToku').length }
      ]} />
      {kartica === 'rezervacije' && (rezervacije.ucitava ? <Ucitavanje /> : rezervacije.greska ? <Greska poruka={rezervacije.greska} /> : rezervacije.podaci?.stavke.length ? (
        <div className="space-y-3">
          {[...rezervacije.podaci.stavke]
            .sort((a, b) => (a.id === Number(parametar) ? -1 : b.id === Number(parametar) ? 1 : 0))
            .map((r) => <KarticaRezervacije key={r.id} r={r} osvezi={rezervacije.ponovo} />)}
        </div>
      ) : <Prazno naslov="Nema rezervacija" opis="Rezervacija nastaje kada prodavac prihvati ponudu ili rezerviše oglas za vas." />)}
      {kartica === 'transakcije' && (transakcije.ucitava ? <Ucitavanje /> : transakcije.greska ? <Greska poruka={transakcije.greska} /> : transakcije.podaci?.stavke.length ? (
        <div className="space-y-2">
          {transakcije.podaci.stavke.map((t) => (
            <Link key={t.id} to={`/transakcije/${t.id}`} className="kartica flex flex-wrap items-center gap-3 p-3 hover:shadow-md">
              <span className="h-12 w-12 shrink-0 overflow-hidden rounded-lg"><SlikaIliZamena putanja={t.oglas.glavnaSlika} naslov={t.oglas.naslov} klasa="h-12" /></span>
              <span className="min-w-0 flex-1">
                <span className="block font-semibold">{t.oglas.naslov}</span>
                <span className="text-xs text-slate-500">{t.kupacId === korisnik!.id ? 'Kupovina' : 'Prodaja'} · {cena(t.cena, t.oglas.tip)} · {relativno(t.kreiranoUtc)}</span>
              </span>
              <Znacka>{nazivEscrowa[t.statusEscrowa]}</Znacka>
              <Znacka boja={t.status === 'Zavrsena' ? 'bg-green-100 text-green-800' : t.status === 'Otkazana' ? 'bg-red-100 text-red-800' : 'bg-amber-100 text-amber-800'}>
                {nazivStatusaTransakcije[t.status]}
              </Znacka>
            </Link>
          ))}
        </div>
      ) : <Prazno naslov="Nema transakcija" />)}
    </div>
  )
}
