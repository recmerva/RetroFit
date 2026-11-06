import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { ShieldCheck, UserPlus } from 'lucide-react'
import { greskePolja, porukaGreske } from '../api/klijent'
import { useAuth } from '../kontekst/AuthKontekst'
import { useToast } from '../kontekst/ToastKontekst'
import { Polje } from '../komponente/Ui'
import { useGradovi } from '../pomocno/kuke'

/** Klijentska validacija (ista pravila kao FluentValidation na serveru - server je i dalje glavni čuvar). */
function validiraj(p: { email: string; lozinka: string; potvrda: string; ime: string; prezime: string }) {
  const g: Record<string, string> = {}
  if (!/^\S+@\S+\.\S+$/.test(p.email)) g.email = 'Email adresa nije ispravna.'
  if (p.lozinka.length < 8) g.lozinka = 'Lozinka mora imati najmanje 8 karaktera.'
  else if (!/[a-zA-ZčćšđžČĆŠĐŽ]/.test(p.lozinka) || !/\d/.test(p.lozinka)) g.lozinka = 'Lozinka mora sadržati bar jedno slovo i bar jednu cifru.'
  if (p.potvrda !== p.lozinka) g.potvrda = 'Lozinke se ne poklapaju.'
  if (p.ime.trim().length < 2) g.ime = 'Ime mora imati najmanje 2 karaktera.'
  if (p.prezime.trim().length < 2) g.prezime = 'Prezime mora imati najmanje 2 karaktera.'
  return g
}

export default function Registracija() {
  const { registruj } = useAuth()
  const toast = useToast()
  const navigacija = useNavigate()
  const gradovi = useGradovi()
  const [p, setP] = useState({ email: '', lozinka: '', potvrda: '', ime: '', prezime: '', gradId: '' })
  const [greske, setGreske] = useState<Record<string, string>>({})
  const [opsta, setOpsta] = useState<string | null>(null)
  const [salje, setSalje] = useState(false)

  const posalji = async (e: FormEvent) => {
    e.preventDefault()
    const g = validiraj(p)
    setGreske(g)
    if (Object.keys(g).length) return
    setSalje(true)
    setOpsta(null)
    try {
      await registruj({ email: p.email.trim(), lozinka: p.lozinka, ime: p.ime.trim(), prezime: p.prezime.trim(), gradId: p.gradId ? Number(p.gradId) : undefined })
      toast.uspeh('Dobro došli na RetroFit!')
      navigacija('/profil')
    } catch (err) {
      setGreske(greskePolja(err))
      setOpsta(porukaGreske(err))
    } finally {
      setSalje(false)
    }
  }

  const polje = (k: keyof typeof p) => ({ value: p[k], onChange: (e: { target: { value: string } }) => setP({ ...p, [k]: e.target.value }) })

  return (
    <div className="mx-auto max-w-lg">
      <form onSubmit={posalji} className="kartica space-y-4 p-6" noValidate>
        <div className="text-center">
          <h1 className="text-2xl font-bold">Registracija</h1>
          <p className="text-sm text-slate-500">Napravite nalog i počnite da kupujete i prodajete</p>
        </div>
        {opsta && <p className="rounded-lg bg-red-50 p-3 text-sm text-red-700" role="alert">{opsta}</p>}
        <div className="grid gap-4 sm:grid-cols-2">
          <Polje oznaka="Ime" greska={greske.ime}><input className="unos" autoComplete="given-name" {...polje('ime')} /></Polje>
          <Polje oznaka="Prezime" greska={greske.prezime}><input className="unos" autoComplete="family-name" {...polje('prezime')} /></Polje>
        </div>
        <Polje oznaka="Email" greska={greske.email}><input className="unos" type="email" autoComplete="email" {...polje('email')} /></Polje>
        <Polje oznaka="Grad (opciono)">
          <select className="unos" {...polje('gradId')}>
            <option value="">-- izaberite --</option>
            {gradovi.map((g) => <option key={g.id} value={g.id}>{g.naziv}</option>)}
          </select>
        </Polje>
        <Polje oznaka="Lozinka" greska={greske.lozinka} pomoc="Najmanje 8 karaktera, bar jedno slovo i jedna cifra.">
          <input className="unos" type="password" autoComplete="new-password" {...polje('lozinka')} />
        </Polje>
        <Polje oznaka="Potvrda lozinke" greska={greske.potvrda}><input className="unos" type="password" autoComplete="new-password" {...polje('potvrda')} /></Polje>
        <button className="dugme-primarno w-full" disabled={salje}>
          <UserPlus className="h-4 w-4" /> {salje ? 'Kreiranje naloga...' : 'Registruj se'}
        </button>
        <p className="flex items-start gap-2 text-xs text-slate-500">
          <ShieldCheck className="h-4 w-4 shrink-0 text-green-600" />
          Radi zaštite od lažnih naloga, pregledač automatski rešava kratku bezbednosnu proveru (bez CAPTCHA slika).
        </p>
        <p className="text-center text-sm text-slate-600">
          Već imate nalog? <Link to="/prijava" className="font-semibold text-retro-700 hover:underline">Prijavite se</Link>
        </p>
      </form>
    </div>
  )
}
