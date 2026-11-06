import { createContext, useCallback, useContext, useState, type ReactNode } from 'react'
import { CheckCircle2, AlertTriangle, X } from 'lucide-react'

interface Toast {
  id: number
  tekst: string
  vrsta: 'uspeh' | 'greska'
}

const ToastKontekst = createContext<{ uspeh: (t: string) => void; greska: (t: string) => void } | null>(null)

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toastovi, setToastovi] = useState<Toast[]>([])

  const dodaj = useCallback((tekst: string, vrsta: Toast['vrsta']) => {
    const id = Date.now() + Math.random()
    setToastovi((t) => [...t, { id, tekst, vrsta }])
    setTimeout(() => setToastovi((t) => t.filter((x) => x.id !== id)), 5000)
  }, [])

  const uspeh = useCallback((t: string) => dodaj(t, 'uspeh'), [dodaj])
  const greska = useCallback((t: string) => dodaj(t, 'greska'), [dodaj])

  return (
    <ToastKontekst.Provider value={{ uspeh, greska }}>
      {children}
      <div className="fixed right-4 bottom-4 z-50 flex w-80 flex-col gap-2" aria-live="polite">
        {toastovi.map((t) => (
          <div
            key={t.id}
            className={`flex items-start gap-2 rounded-lg p-3 text-sm text-white shadow-lg ${t.vrsta === 'uspeh' ? 'bg-green-700' : 'bg-red-700'}`}
          >
            {t.vrsta === 'uspeh' ? <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0" /> : <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0" />}
            <span className="flex-1">{t.tekst}</span>
            <button onClick={() => setToastovi((x) => x.filter((y) => y.id !== t.id))} aria-label="Zatvori">
              <X className="h-4 w-4" />
            </button>
          </div>
        ))}
      </div>
    </ToastKontekst.Provider>
  )
}

export function useToast() {
  const k = useContext(ToastKontekst)
  if (!k) throw new Error('useToast mora biti unutar ToastProvider-a')
  return k
}
