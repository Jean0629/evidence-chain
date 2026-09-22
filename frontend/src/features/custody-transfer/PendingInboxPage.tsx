import { Link, Navigate, useNavigate } from 'react-router-dom'
import { toApiError } from '../../api/errors'
import { useAuth } from '../../auth/AuthContext'
import { IntegrityBadge } from '../../components/IntegrityBadge'
import { formatDateTime } from '../../lib/format'
import { useMyPendingTransfers } from './useMyPendingTransfers'

const SKELETON_ROWS = 4

export function PendingInboxPage() {
  const { role } = useAuth()
  const navigate = useNavigate()
  const pending = useMyPendingTransfers()
  const items = pending.data ?? []

  // La bandeja de pendientes existe solo para Custodios (el API responde 403 al resto).
  if (role !== 'Custodio') return <Navigate to="/evidence" replace />

  const error = pending.isError ? toApiError(pending.error) : null

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-xl font-semibold">Mis pendientes</h1>
        <p className="text-sm text-slate-500">
          Transferencias de custodia que esperan tu aceptación o rechazo, de la más antigua a la más reciente.
        </p>
      </div>

      {error ? (
        <div role="alert" className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
          <p className="font-medium">No se pudieron cargar tus pendientes.</p>
          <p className="mt-1">{error.message}</p>
          <button
            type="button"
            onClick={() => void pending.refetch()}
            className="mt-3 rounded-md border border-red-300 bg-white px-3 py-1 text-red-700 hover:bg-red-100"
          >
            Reintentar
          </button>
        </div>
      ) : (
        <div className="overflow-hidden rounded-lg border border-slate-200 bg-white">
          <table className="min-w-full divide-y divide-slate-200 text-sm" aria-busy={pending.isFetching}>
            <thead className="bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500">
              <tr>
                <th scope="col" className="px-4 py-3 font-medium">Evidencia</th>
                <th scope="col" className="px-4 py-3 font-medium">Solicitada por</th>
                <th scope="col" className="px-4 py-3 font-medium">Fecha de solicitud</th>
                <th scope="col" className="px-4 py-3 font-medium">Integridad</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {pending.isPending &&
                Array.from({ length: SKELETON_ROWS }, (_, i) => (
                  <tr key={i} aria-hidden="true">
                    {Array.from({ length: 4 }, (_, j) => (
                      <td key={j} className="px-4 py-3">
                        <div className="h-4 animate-pulse rounded bg-slate-200" />
                      </td>
                    ))}
                  </tr>
                ))}

              {pending.isSuccess && items.length === 0 && (
                <tr>
                  <td colSpan={4} className="px-4 py-12 text-center text-slate-500">
                    No tienes transferencias pendientes.
                  </td>
                </tr>
              )}

              {items.map((item) => (
                <tr
                  key={item.id}
                  onClick={() => navigate(`/evidence/${item.evidenceId}`)}
                  className="cursor-pointer hover:bg-slate-50"
                >
                  <td className="px-4 py-3 font-mono font-medium">
                    <Link
                      to={`/evidence/${item.evidenceId}`}
                      onClick={(e) => e.stopPropagation()}
                      className="text-slate-900 underline-offset-2 hover:underline"
                    >
                      {item.evidenceCode}
                    </Link>
                  </td>
                  <td className="px-4 py-3 text-slate-700">{item.fromCustodianName}</td>
                  <td className="whitespace-nowrap px-4 py-3 text-slate-600">
                    {formatDateTime(item.requestedAtUtc)}
                  </td>
                  <td className="px-4 py-3">
                    <IntegrityBadge valid={item.isIntegrityValid} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
