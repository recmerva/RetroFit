import { useState, type FormEvent } from 'react'
import { Link, useLocation, useNavigate } from 'react-router'
import { LogIn } from 'lucide-react'
import { porukaGreske } from '../api/klijent'
import { useAuth } from '../kontekst/AuthKontekst'
import { Polje } from '../komponente/Ui'

export default function Prijava() {
  const { prijavi } = useAuth()
  const navigacija = useNavigate()
  const lokacija = useLocation()
  const [email, setEmail] = useState('')
  const [lozinka, setLozinka] = useState('')
  const [greska, setGreska] = useState<string | null>(null)
  const [salje, setSalje] = useState(false)

  const posalji = async (e: FormEvent) => {
    e.preventDefault()
    if (!email.trim() || !lozinka) {
      setGreska('Unesite email i lozinku.')
      return
    }
    setSalje(true)
    setGreska(null)
    try {
      await prijavi(email.trim(), lozinka)
      const nazad = (lokacija.state as { nazad?: string } | null)?.nazad
      navigacija(nazad ?? '/', { replace: true })
    } catch (err) {
      setGreska(porukaGreske(err))
    } finally {
      setSalje(false)
    }
  }

  return (
    <div className="mx-auto max-w-md">
      <form onSubmit={posalji} className="kartica space-y-4 p-6" noValidate>
        <div className="text-center">
          <h1 className="text-2xl font-bold">Prijava</h1>
          <p className="text-sm text-slate-500">Dobro došli nazad na RetroFit</p>
        </div>
        {greska && <p className="rounded-lg bg-red-50 p-3 text-sm text-red-700" role="alert">{greska}</p>}
        <Polje oznaka="Email">
          <input className="unos" type="email" autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
        </Polje>
        <Polje oznaka="Lozinka">
          <input className="unos" type="password" autoComplete="current-password" value={lozinka} onChange={(e) => setLozinka(e.target.value)} required />
        </Polje>
        <button className="dugme-primarno w-full" disabled={salje}>
          <LogIn className="h-4 w-4" /> {salje ? 'Prijavljivanje...' : 'Prijavi se'}
        </button>
        <p className="text-center text-sm text-slate-600">
          Nemate nalog? <Link to="/registracija" className="font-semibold text-retro-700 hover:underline">Registrujte se</Link>
        </p>
        <div className="rounded-lg bg-slate-50 p-3 text-xs text-slate-500">
          <p className="font-semibold">Demo nalozi:</p>
          <p>marko@retrofit.rs · ana@retrofit.rs / Lozinka123</p>
          <p>moderator@retrofit.rs / Moderator123! · admin@retrofit.rs / Admin123!</p>
        </div>
      </form>
    </div>
  )
}
