import { Link, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { useMyPendingTransfers } from '../features/custody-transfer/useMyPendingTransfers'

export function AppLayout() {
  const { displayName, role, logout } = useAuth()
  const navigate = useNavigate()
  const pending = useMyPendingTransfers()

  function handleLogout() {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <div className="min-h-screen">
      <header className="sticky top-0 z-40 border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-3">
          <Link to="/evidence" className="text-base font-semibold text-slate-900">
            Plataforma de evidencia digital
          </Link>
          <div className="flex items-center gap-3 text-sm">
            {role === 'Custodio' && (
              <Link
                to="/pending"
                className="flex items-center gap-2 rounded-md border border-slate-300 px-3 py-1 text-slate-700 hover:bg-slate-50"
              >
                Mis pendientes
                {pending.isSuccess && (
                  <span
                    aria-label={`${pending.data.length} ${pending.data.length === 1 ? 'pendiente' : 'pendientes'}`}
                    className={`min-w-5 rounded-full px-1.5 text-center text-xs font-semibold ${
                      pending.data.length > 0 ? 'bg-sky-600 text-white' : 'bg-slate-100 text-slate-500'
                    }`}
                  >
                    {pending.data.length}
                  </span>
                )}
              </Link>
            )}
            <span className="text-slate-700">{displayName}</span>
            <span className="rounded-full bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-600">
              {role}
            </span>
            <button
              type="button"
              onClick={handleLogout}
              className="rounded-md border border-slate-300 px-3 py-1 text-slate-700 hover:bg-slate-50"
            >
              Cerrar sesión
            </button>
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-4 py-6">
        <Outlet />
      </main>
    </div>
  )
}
