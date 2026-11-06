import { useEffect, useRef, useState, type FormEvent, type ReactNode } from 'react'
import { Link, NavLink, Outlet, useLocation, useNavigate } from 'react-router'
import {
  Bell, ChevronDown, Handshake, Heart, LogOut, Menu, MessageCircle, PackageSearch, Plus, Receipt, Search, Shield, ShieldCheck, User, X
} from 'lucide-react'
import { useAuth } from '../kontekst/AuthKontekst'
import { useBrojaci } from '../pomocno/kuke'

function Logo() {
  return (
    <Link to="/" className="flex items-center gap-2 text-xl font-black tracking-tight text-white">
      <span className="flex h-8 w-8 items-center justify-center rounded-full border-4 border-retro-500 bg-noc-900">
        <span className="h-2 w-2 rounded-full bg-retro-300" />
      </span>
      Retro<span className="text-retro-400">Fit</span>
    </Link>
  )
}

function IkonicaSaBrojem({ to, ikonica, broj, oznaka }: { to: string; ikonica: ReactNode; broj?: number; oznaka: string }) {
  return (
    <NavLink to={to} className="relative rounded-lg p-2 text-slate-200 hover:bg-noc-700 hover:text-white" aria-label={oznaka} title={oznaka}>
      {ikonica}
      {broj ? (
        <span className="absolute -top-0.5 -right-0.5 min-w-5 rounded-full bg-retro-500 px-1 text-center text-[11px] leading-5 font-bold text-white">
          {broj > 99 ? '99+' : broj}
        </span>
      ) : null}
    </NavLink>
  )
}

export function Layout() {
  const { korisnik, odjavi, imaUlogu } = useAuth()
  const brojaci = useBrojaci(!!korisnik)
  const navigacija = useNavigate()
  const lokacija = useLocation()
  const [upit, setUpit] = useState('')
  const [meniOtvoren, setMeniOtvoren] = useState(false)
  const [mobilni, setMobilni] = useState(false)
  const meniRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    setMeniOtvoren(false)
    setMobilni(false)
  }, [lokacija.pathname])

  useEffect(() => {
    const klik = (e: MouseEvent) => meniRef.current && !meniRef.current.contains(e.target as Node) && setMeniOtvoren(false)
    document.addEventListener('mousedown', klik)
    return () => document.removeEventListener('mousedown', klik)
  }, [])

  const pretrazi = (e: FormEvent) => {
    e.preventDefault()
    navigacija(`/oglasi?upit=${encodeURIComponent(upit.trim())}`)
  }

  const odjava = async () => {
    await odjavi()
    navigacija('/')
  }

  const stavkeMenija = korisnik
    ? [
        { to: '/profil', naziv: 'Moj profil i oglasi', ikonica: <User className="h-4 w-4" /> },
        { to: '/ponude', naziv: 'Ponude', ikonica: <Handshake className="h-4 w-4" />, broj: brojaci?.ponudeNaCekanju },
        { to: '/transakcije', naziv: 'Kupovine i prodaje', ikonica: <Receipt className="h-4 w-4" /> },
        { to: '/omiljeni', naziv: 'Omiljeni oglasi', ikonica: <Heart className="h-4 w-4" /> },
        ...(imaUlogu('Moderator') || imaUlogu('Admin') ? [{ to: '/moderacija', naziv: 'Moderacija', ikonica: <Shield className="h-4 w-4" /> }] : []),
        ...(imaUlogu('Admin') ? [{ to: '/admin', naziv: 'Administracija', ikonica: <ShieldCheck className="h-4 w-4" /> }] : [])
      ]
    : []

  return (
    <div className="flex min-h-screen flex-col">
      <header className="sticky top-0 z-30 bg-noc-800 shadow">
        <div className="mx-auto flex max-w-7xl items-center gap-3 px-4 py-3">
          <Logo />
          <form onSubmit={pretrazi} className="relative mx-2 hidden flex-1 md:block" role="search">
            <Search className="absolute top-2.5 left-3 h-4 w-4 text-slate-400" />
            <input
              value={upit}
              onChange={(e) => setUpit(e.target.value)}
              placeholder="Šta tražite? npr. bicikl, gramofon, iPhone..."
              className="w-full rounded-lg border-0 bg-white py-2 pr-3 pl-9 text-sm focus:ring-2 focus:ring-retro-400 focus:outline-none"
              aria-label="Pretraga oglasa"
            />
          </form>
          <nav className="ml-auto hidden items-center gap-1 lg:flex">
            <NavLink to="/oglasi" className="rounded-lg px-3 py-2 text-sm font-medium text-slate-200 hover:bg-noc-700 hover:text-white">
              Oglasi
            </NavLink>
            <NavLink to="/trazim" className="rounded-lg px-3 py-2 text-sm font-medium text-slate-200 hover:bg-noc-700 hover:text-white">
              Tražim...
            </NavLink>
          </nav>
          {korisnik ? (
            <div className="ml-auto flex items-center gap-1 lg:ml-0">
              <Link to="/oglasi/novi" className="dugme-primarno hidden sm:inline-flex">
                <Plus className="h-4 w-4" /> Postavi oglas
              </Link>
              <IkonicaSaBrojem to="/poruke" oznaka="Poruke" ikonica={<MessageCircle className="h-5 w-5" />} broj={brojaci?.neprocitanePoruke} />
              <IkonicaSaBrojem to="/obavestenja" oznaka="Obaveštenja" ikonica={<Bell className="h-5 w-5" />} broj={brojaci?.neprocitanaObavestenja} />
              <div className="relative hidden lg:block" ref={meniRef}>
                <button
                  onClick={() => setMeniOtvoren((o) => !o)}
                  className="flex items-center gap-1 rounded-lg px-2 py-2 text-sm font-medium text-white hover:bg-noc-700"
                  aria-expanded={meniOtvoren}
                >
                  <span className="flex h-7 w-7 items-center justify-center rounded-full bg-retro-500 text-xs font-bold">
                    {korisnik.ime[0]}
                    {korisnik.prezime[0]}
                  </span>
                  <ChevronDown className="h-4 w-4" />
                </button>
                {meniOtvoren && (
                  <div className="kartica absolute right-0 mt-2 w-60 py-2">
                    <p className="border-b border-slate-100 px-4 pb-2 text-sm">
                      <span className="block font-semibold">{korisnik.prikaznoIme}</span>
                      <span className="text-xs text-slate-500">{korisnik.email}</span>
                    </p>
                    {stavkeMenija.map((s) => (
                      <Link key={s.to} to={s.to} className="flex items-center gap-2 px-4 py-2 text-sm hover:bg-slate-50">
                        {s.ikonica}
                        <span className="flex-1">{s.naziv}</span>
                        {s.broj ? <span className="rounded-full bg-retro-100 px-1.5 text-xs text-retro-700">{s.broj}</span> : null}
                      </Link>
                    ))}
                    <button onClick={odjava} className="flex w-full items-center gap-2 border-t border-slate-100 px-4 py-2 text-sm text-red-600 hover:bg-red-50">
                      <LogOut className="h-4 w-4" /> Odjava
                    </button>
                  </div>
                )}
              </div>
            </div>
          ) : (
            <div className="ml-auto flex items-center gap-2 lg:ml-0">
              <Link to="/prijava" className="rounded-lg px-3 py-2 text-sm font-semibold text-white hover:bg-noc-700">
                Prijava
              </Link>
              <Link to="/registracija" className="dugme-primarno hidden sm:inline-flex">
                Registracija
              </Link>
            </div>
          )}
          <button className="rounded-lg p-2 text-white lg:hidden" onClick={() => setMobilni((m) => !m)} aria-label="Meni" aria-expanded={mobilni}>
            {mobilni ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
          </button>
        </div>
        {mobilni && (
          <div className="border-t border-noc-700 bg-noc-800 px-4 py-3 lg:hidden">
            <form onSubmit={pretrazi} className="mb-3" role="search">
              <input value={upit} onChange={(e) => setUpit(e.target.value)} placeholder="Pretraga oglasa..." className="unos" />
            </form>
            {[
              { to: '/oglasi', naziv: 'Oglasi' },
              { to: '/trazim', naziv: 'Tražim...' },
              ...(korisnik ? [{ to: '/oglasi/novi', naziv: '+ Postavi oglas' }] : [{ to: '/registracija', naziv: 'Registracija' }]),
              ...stavkeMenija
            ].map((s) => (
              <Link key={s.to} to={s.to} className="block rounded-lg px-3 py-2 text-sm text-slate-100 hover:bg-noc-700">
                {s.naziv}
              </Link>
            ))}
            {korisnik && (
              <button onClick={odjava} className="mt-1 block w-full rounded-lg px-3 py-2 text-left text-sm text-red-300 hover:bg-noc-700">
                Odjava
              </button>
            )}
          </div>
        )}
      </header>

      <main className="mx-auto w-full max-w-7xl flex-1 px-4 py-6">
        <Outlet />
      </main>

      <footer className="mt-10 bg-noc-900 py-8 text-sm text-slate-400">
        <div className="mx-auto flex max-w-7xl flex-wrap items-center justify-between gap-4 px-4">
          <div className="flex items-center gap-2">
            <PackageSearch className="h-5 w-5 text-retro-400" />
            <span>RetroFit - kupi, prodaj, razmeni ili pokloni. Projekat iz Naprednih baza podataka.</span>
          </div>
          <span>© {new Date().getFullYear()} RetroFit</span>
        </div>
      </footer>
    </div>
  )
}
