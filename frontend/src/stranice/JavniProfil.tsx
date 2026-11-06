import { useCallback, useState } from 'react'
import { useParams } from 'react-router'
import { BadgeCheck, MapPin } from 'lucide-react'
import { korisnici } from '../api/servisi'
import type { OglasKartica } from '../api/tipovi'
import { MrezaOglasa } from '../komponente/KarticaOglasa'
import { Greska, Kartice, Prazno, Stranicenje, Ucitavanje, Znacka, Zvezdice } from '../komponente/Ui'
import { datum, nazivBedza, relativno } from '../pomocno/format'
import { useUpit } from '../pomocno/kuke'
import { usePrebaciOmiljeni } from '../pomocno/omiljeni'

export default function JavniProfil() {
  const id = Number(useParams().id)
  const [kartica, setKartica] = useState<'oglasi' | 'recenzije'>('oglasi')
  const [stranica, setStranica] = useState(1)
  const profil = useUpit(() => korisnici.javniProfil(id), [id])
  const ocene = useUpit(() => korisnici.ocene(id), [id])
  const oglasi = useUpit(() => korisnici.oglasi(id, stranica), [id, stranica])
  const recenzije = useUpit(() => korisnici.recenzije(id), [id])
  const azuriraj = useCallback((fn: (o: OglasKartica) => OglasKartica) => oglasi.setPodaci((s) => (s ? { ...s, stavke: s.stavke.map(fn) } : s)), [oglasi])
  const promeniOmiljeni = usePrebaciOmiljeni(azuriraj)

  if (profil.ucitava) return <Ucitavanje />
  if (profil.greska || !profil.podaci) return <Greska poruka={profil.greska ?? 'Korisnik ne postoji.'} />
  const p = profil.podaci

  return (
    <div className="space-y-6">
      <div className="kartica grid gap-6 p-6 md:grid-cols-[1fr_280px]">
        <div className="flex gap-4">
          <span className="flex h-16 w-16 shrink-0 items-center justify-center rounded-full bg-noc-800 text-2xl font-bold text-white">{p.prikaznoIme[0]}</span>
          <div>
            <h1 className="flex items-center gap-2 text-2xl font-bold">{p.prikaznoIme} {p.telefonVerifikovan && <BadgeCheck className="h-5 w-5 text-blue-600" />}</h1>
            <p className="flex items-center gap-1 text-sm text-slate-500"><MapPin className="h-4 w-4" /> {p.gradNaziv ?? 'Nepoznat grad'} · član od {datum(p.clanOdUtc)}</p>
            <div className="mt-2 flex flex-wrap gap-1">{p.bedzevi.map((b) => <Znacka key={b} boja="bg-blue-50 text-blue-800">{nazivBedza[b] ?? b}</Znacka>)}</div>
            {p.oKorisniku && <p className="mt-3 text-sm text-slate-700">{p.oKorisniku}</p>}
            <p className="mt-3 text-sm text-slate-600">
              <strong>{p.brojAktivnihOglasa}</strong> aktivnih oglasa · <strong>{p.brojZavrsenihTransakcija}</strong> završenih kupoprodaja · reputacija <strong>{p.reputacija}</strong>/100
            </p>
          </div>
        </div>
        {ocene.podaci && (
          <div className="rounded-lg bg-slate-50 p-4">
            <p className="text-3xl font-black">{ocene.podaci.prosecnaOcena.toFixed(1)}</p>
            <Zvezdice ocena={ocene.podaci.prosecnaOcena} broj={ocene.podaci.brojRecenzija} />
            <div className="mt-3 space-y-1">
              {[5, 4, 3, 2, 1].map((o) => {
                const broj = ocene.podaci!.raspodelaOcena[o] ?? 0
                const procenat = ocene.podaci!.brojRecenzija ? (broj / ocene.podaci!.brojRecenzija) * 100 : 0
                return (
                  <div key={o} className="flex items-center gap-2 text-xs">
                    <span className="w-3">{o}</span>
                    <div className="h-2 flex-1 overflow-hidden rounded bg-slate-200"><div className="h-full bg-amber-400" style={{ width: `${procenat}%` }} /></div>
                    <span className="w-6 text-right text-slate-500">{broj}</span>
                  </div>
                )
              })}
            </div>
          </div>
        )}
      </div>

      <Kartice aktivna={kartica} promeni={setKartica} kartice={[
        { kljuc: 'oglasi', naziv: 'Aktivni oglasi', broj: p.brojAktivnihOglasa }, { kljuc: 'recenzije', naziv: 'Recenzije', broj: p.brojRecenzija }
      ]} />

      {kartica === 'oglasi' && (oglasi.ucitava ? <Ucitavanje /> : oglasi.podaci?.stavke.length ? (
        <>
          <MrezaOglasa oglasi={oglasi.podaci.stavke} promeniOmiljeni={promeniOmiljeni} />
          <Stranicenje stranica={oglasi.podaci.stranica} ukupnoStranica={oglasi.podaci.ukupnoStranica} promeni={setStranica} />
        </>
      ) : <Prazno naslov="Korisnik trenutno nema aktivnih oglasa" />)}

      {kartica === 'recenzije' && (recenzije.ucitava ? <Ucitavanje /> : recenzije.podaci?.stavke.length ? (
        <div className="space-y-3">
          {recenzije.podaci.stavke.map((r) => (
            <div key={r.id} className="kartica p-4">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <span className="font-semibold">{r.ocenjivacIme} <span className="text-xs font-normal text-slate-500">({r.ulogaOcenjivaca})</span></span>
                <Zvezdice ocena={r.ocena} />
              </div>
              {r.komentar && <p className="mt-1 text-sm text-slate-700">{r.komentar}</p>}
              <p className="mt-1 text-xs text-slate-400">{r.oglasNaslov} · {relativno(r.kreiranoUtc)}</p>
            </div>
          ))}
        </div>
      ) : <Prazno naslov="Još nema recenzija" />)}
    </div>
  )
}
