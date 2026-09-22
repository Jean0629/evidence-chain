import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'
import type { EvidenceListParams, SortOrder } from '../../api/types'

type FilterPatch = Partial<Pick<EvidenceListParams, 'search' | 'custodianId' | 'sort'>>

/**
 * Filtros, orden y cursor de la bandeja, persistidos en los query params de la URL
 * para que la vista sea recargable y compartible.
 */
export function useEvidenceListFilters() {
  const [searchParams, setSearchParams] = useSearchParams()

  const params = useMemo<EvidenceListParams>(
    () => ({
      search: searchParams.get('search') ?? '',
      custodianId: searchParams.get('custodianId') ?? '',
      cursor: searchParams.get('cursor'),
      sort: searchParams.get('sort') === 'asc' ? 'asc' : 'desc',
    }),
    [searchParams],
  )

  /** Cambiar cualquier filtro u orden descarta el cursor (vuelve a la primera página). */
  const setFilters = useCallback(
    (patch: FilterPatch, options?: { replace?: boolean }) => {
      setSearchParams(
        (prev) => {
          const next = new URLSearchParams(prev)
          for (const [key, value] of Object.entries(patch)) {
            const isDefault = key === 'sort' ? (value as SortOrder) === 'desc' : !value
            if (isDefault) next.delete(key)
            else next.set(key, value)
          }
          next.delete('cursor')
          return next
        },
        { replace: options?.replace },
      )
    },
    [setSearchParams],
  )

  const setCursor = useCallback(
    (cursor: string | null) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev)
        if (cursor) next.set('cursor', cursor)
        else next.delete('cursor')
        return next
      })
    },
    [setSearchParams],
  )

  return { params, setFilters, setCursor }
}
