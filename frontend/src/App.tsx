import { Route, Routes } from 'react-router'
import { Layout } from './komponente/Layout'
import { ZasticenaRuta } from './komponente/ZasticenaRuta'
import { Prazno } from './komponente/Ui'
import Pocetna from './stranice/Pocetna'
import Prijava from './stranice/Prijava'
import Registracija from './stranice/Registracija'
import Oglasi from './stranice/Oglasi'
import OglasDetalji from './stranice/OglasDetalji'
import FormaOglasa from './stranice/FormaOglasa'
import Profil from './stranice/Profil'
import JavniProfil from './stranice/JavniProfil'
import Poruke from './stranice/Poruke'
import Ponude from './stranice/Ponude'
import Transakcije from './stranice/Transakcije'
import Omiljeni from './stranice/Omiljeni'
import Trazim from './stranice/Trazim'
import ZahtevDetalji from './stranice/ZahtevDetalji'
import Obavestenja from './stranice/Obavestenja'
import AdminPanel from './stranice/AdminPanel'
import ModeratorPanel from './stranice/ModeratorPanel'

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<Pocetna />} />
        <Route path="prijava" element={<Prijava />} />
        <Route path="registracija" element={<Registracija />} />
        <Route path="oglasi" element={<Oglasi />} />
        <Route path="oglasi/:id" element={<OglasDetalji />} />
        <Route path="korisnici/:id" element={<JavniProfil />} />
        <Route path="trazim" element={<Trazim />} />
        <Route path="trazim/:id" element={<ZahtevDetalji />} />

        <Route path="oglasi/novi" element={<ZasticenaRuta><FormaOglasa /></ZasticenaRuta>} />
        <Route path="oglasi/:id/izmena" element={<ZasticenaRuta><FormaOglasa /></ZasticenaRuta>} />
        <Route path="profil" element={<ZasticenaRuta><Profil /></ZasticenaRuta>} />
        <Route path="poruke" element={<ZasticenaRuta><Poruke /></ZasticenaRuta>} />
        <Route path="razgovori/:id" element={<ZasticenaRuta><Poruke /></ZasticenaRuta>} />
        <Route path="ponude" element={<ZasticenaRuta><Ponude /></ZasticenaRuta>} />
        <Route path="transakcije" element={<ZasticenaRuta><Transakcije /></ZasticenaRuta>} />
        <Route path="transakcije/:id" element={<ZasticenaRuta><Transakcije /></ZasticenaRuta>} />
        <Route path="rezervacije/:id" element={<ZasticenaRuta><Transakcije /></ZasticenaRuta>} />
        <Route path="omiljeni" element={<ZasticenaRuta><Omiljeni /></ZasticenaRuta>} />
        <Route path="obavestenja" element={<ZasticenaRuta><Obavestenja /></ZasticenaRuta>} />
        <Route path="moderacija" element={<ZasticenaRuta uloge={['Moderator', 'Admin']}><ModeratorPanel /></ZasticenaRuta>} />
        <Route path="admin" element={<ZasticenaRuta uloge={['Admin']}><AdminPanel /></ZasticenaRuta>} />

        <Route path="*" element={<Prazno naslov="Stranica ne postoji" opis="Proverite adresu ili se vratite na početnu stranu." />} />
      </Route>
    </Routes>
  )
}
