import { useQuery } from '@tanstack/react-query'
import { api } from '../../api/client'
import { transferKeys } from '../../api/queryKeys'
import type { MyPendingTransfer } from '../../api/types'
import { useAuth } from '../../auth/AuthContext'

const REFRESH_INTERVAL_MS = 60_000

/**
 * Transferencias pendientes dirigidas al Custodio en sesión. El endpoint es solo para
 * Custodio, así que para otros roles la query queda deshabilitada (sin llamada al API).
 */
export function useMyPendingTransfers() {
  const { role } = useAuth()

  return useQuery({
    queryKey: transferKeys.pending,
    queryFn: async ({ signal }) => {
      const { data } = await api.get<MyPendingTransfer[]>('/custody-transfers', { signal })
      return data
    },
    enabled: role === 'Custodio',
    // Es la fuente del contador del header: se mantiene razonablemente fresca.
    refetchOnWindowFocus: true,
    refetchInterval: REFRESH_INTERVAL_MS,
  })
}
