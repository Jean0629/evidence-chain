import { useQuery } from '@tanstack/react-query'
import { api } from '../../api/client'
import { transferKeys } from '../../api/queryKeys'
import type { MyPendingTransfer } from '../../api/types'
import { useAuth } from '../../auth/AuthContext'

const REFRESH_INTERVAL_MS = 60_000

export function useMyPendingTransfers() {
  const { role } = useAuth()

  return useQuery({
    queryKey: transferKeys.pending,
    queryFn: async ({ signal }) => {
      const { data } = await api.get<MyPendingTransfer[]>('/custody-transfers', { signal })
      return data
    },
    enabled: role === 'Custodio',
    refetchOnWindowFocus: true,
    refetchInterval: REFRESH_INTERVAL_MS,
  })
}
