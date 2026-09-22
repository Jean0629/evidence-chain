import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { toApiError } from '../../api/errors'
import type { SortOrder } from '../../api/types'
import { useCustodians } from '../../api/useCustodians'
import { IntegrityBadge } from '../../components/IntegrityBadge'
import { custodianOptionLabel, formatDateTime } from '../../lib/format'
import { useEvidenceList } from './useEvidenceList'
import { useEvidenceListFilters } from './useEvidenceListFilters'

const SEARCH_DEBOUNCE_MS = 300
const SKELETON_ROWS = 6

const inputClass =
  'block w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm focus:border-slate-500 focus:outline-none focus:ring-1 focus:ring-slate-500'

export function EvidenceListPage() {
  const navigate = useNavigate()
  const { params, setFilters, setCursor } = useEvidenceListFilters()
  const custodians = useCustodians()
  const list = useEvidenceList(params)

  // El texto se edita localmente y se vuelca a la URL con debounce; así cada
  // pulsación no dispara una búsqueda ni llena el historial del navegador.
  const [searchText, setSearchText] = useState(params.search)
  const [syncedSearch, setSyncedSearch] = useState(params.search)
  if (params.search !== syncedSearch) {
    // La URL cambió por fuera (botón atrás, enlace compartido): se refleja en el input.
    setSyncedSearch(params.search)
    setSearchText(params.search)
  }

  useEffect(() => {
    if (searchText === params.search) return
    const timer = setTimeout(() => setFilters({ search: searchText }, { replace: true }), SEARCH_DEBOUNCE_MS)
    return () => clearTimeout(timer)
  }, [searchText, params.search, setFilters])

  const items = list.data?.items ?? []
  const nextCursor = list.data?.nextCursor ?? null
  const error = list.isError ? toApiError(list.error) : null

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-xl font-semibold">Evidencias</h1>
        <p className="text-sm text-slate-500">Revisa el estado y la custodia de cada evidencia.</p>
      </div>

      <div className="grid gap-3 rounded-lg border border-slate-200 bg-white p-4 sm:grid-cols-3">
        <div>
          <label htmlFor="search" className="mb-1 block text-xs font-medium text-slate-600">
            Buscar
          </label>
          <input
            id="search"
            type="search"
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            placeholder="Código o descripción"
            className={inputClass}
          />
        </div>
        <div>
          <label htmlFor="custodian" className="mb-1 block text-xs font-medium text-slate-600">
            Custodio actual
          </label>
          <select
            id="custodian"
            value={params.custodianId}
            onChange={(e) => setFilters({ custodianId: e.target.value })}
            className={inputClass}
          >
            <option value="">Todos</option>
            {custodians.data
              // Los Supervisores son de solo lectura: nunca tienen evidencias en custodia.
              ?.filter((c) => c.role === 'Investigador' || c.role === 'Custodio')
              .map((c) => (
                <option key={c.id} value={c.id}>
                  {custodianOptionLabel(c)}
                </option>
              ))}
          </select>
        </div>
        <div>
          <label htmlFor="sort" className="mb-1 block text-xs font-medium text-slate-600">
            Orden por fecha
          </label>
          <select
            id="sort"
            value={params.sort}
            onChange={(e) => setFilters({ sort: e.target.value as SortOrder })}
            className={inputClass}
          >
            <option value="desc">Más recientes primero</option>
            <option value="asc">Más antiguas primero</option>
          </select>
        </div>
      </div>

      {error ? (
        <div role="alert" className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
          <p className="font-medium">No se pudieron cargar las evidencias.</p>
          <p className="mt-1">{error.message}</p>
          <button
            type="button"
            onClick={() => void list.refetch()}
            className="mt-3 rounded-md border border-red-300 bg-white px-3 py-1 text-red-700 hover:bg-red-100"
          >
            Reintentar
          </button>
        </div>
      ) : (
        <div className="overflow-hidden rounded-lg border border-slate-200 bg-white">
          <table className="min-w-full divide-y divide-slate-200 text-sm" aria-busy={list.isFetching}>
            <thead className="bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500">
              <tr>
                <th scope="col" className="px-4 py-3 font-medium">Código</th>
                <th scope="col" className="px-4 py-3 font-medium">Descripción</th>
                <th scope="col" className="px-4 py-3 font-medium">Custodio actual</th>
                <th scope="col" className="px-4 py-3 font-medium">Último evento</th>
                <th scope="col" className="px-4 py-3 font-medium">Integridad</th>
              </tr>
            </thead>
            <tbody
              className={`divide-y divide-slate-100 transition-opacity ${list.isPlaceholderData ? 'opacity-60' : ''}`}
            >
              {list.isPending &&
                Array.from({ length: SKELETON_ROWS }, (_, i) => (
                  <tr key={i} aria-hidden="true">
                    {Array.from({ length: 5 }, (_, j) => (
                      <td key={j} className="px-4 py-3">
                        <div className="h-4 animate-pulse rounded bg-slate-200" />
                      </td>
                    ))}
                  </tr>
                ))}

              {list.isSuccess && items.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-4 py-12 text-center text-slate-500">
                    No se encontraron evidencias con estos filtros.
                  </td>
                </tr>
              )}

              {items.map((item) => (
                <tr
                  key={item.id}
                  onClick={() => navigate(`/evidence/${item.id}`)}
                  className="cursor-pointer hover:bg-slate-50"
                >
                  <td className="px-4 py-3 font-mono font-medium">
                    <Link
                      to={`/evidence/${item.id}`}
                      onClick={(e) => e.stopPropagation()}
                      className="text-slate-900 underline-offset-2 hover:underline"
                    >
                      {item.code}
                    </Link>
                  </td>
                  <td className="max-w-md truncate px-4 py-3 text-slate-700">{item.description}</td>
                  <td className="px-4 py-3 text-slate-700">{item.currentCustodianName}</td>
                  <td className="whitespace-nowrap px-4 py-3 text-slate-600">
                    {formatDateTime(item.lastEventAtUtc)}
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

      {!error && (
        <div className="flex items-center justify-between">
          <div>
            {params.cursor && (
              <button
                type="button"
                onClick={() => setCursor(null)}
                className="rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm text-slate-700 hover:bg-slate-50"
              >
                Volver al inicio
              </button>
            )}
          </div>
          <button
            type="button"
            disabled={!nextCursor || list.isPlaceholderData}
            onClick={() => setCursor(nextCursor)}
            className="rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm text-slate-700 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
          >
            Siguiente
          </button>
        </div>
      )}
    </div>
  )
}
