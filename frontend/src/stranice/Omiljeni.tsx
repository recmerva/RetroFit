import { useCallback, useState } from 'react'
import { Link } from 'react-router'
import { omiljeni } from '../api/servisi'
import type { OglasKartica } from '../api/tipovi'
import { MrezaOglasa } from '../komponente/KarticaOglasa'
import { Greska, Naslov, Prazno, Stranicenje, Ucitavanje } from '../komponente/Ui'
import { useUpit } from '../pomocno/kuke'
import { usePrebaciOmiljeni } from '../pomocno/omiljeni'

export default function Omiljeni() {
  const [stranica, setStranica] = useState(1)
  const r = useUpit(() => omiljeni.lista(stranica), [stranica])
  // Kada se srce isključi, oglas nestaje sa liste omiljenih.
  const azuriraj = useCallback(
    (fn: (o: OglasKartica) => OglasKartica) =>
      r.setPodaci((s) => (s ? { ...s, stavke: s.stavke.map(fn).filter((o) => o.jeOmiljen) } : s)),
    [r]
  )
  const promeni = usePrebaciOmiljeni(azuriraj)

  return (
    <div>
      <Naslov>Omiljeni oglasi</Naslov>
      {r.ucitava ? <Ucitavanje /> : r.greska ? <Greska poruka={r.greska} ponovo={r.ponovo} /> : r.podaci?.stavke.length ? (
        <>
          <MrezaOglasa oglasi={r.podaci.stavke} promeniOmiljeni={promeni} prikaziStatus />
          <Stranicenje stranica={r.podaci.stranica} ukupnoStranica={r.podaci.ukupnoStranica} promeni={setStranica} />
        </>
      ) : <Prazno naslov="Lista želja je prazna" opis="Kliknite na srce na kartici oglasa da ga sačuvate ovde." akcija={<Link to="/oglasi" className="dugme-primarno">Pregledaj oglase</Link>} />}
    </div>
  )
}
