import { useQuery } from '@tanstack/react-query'
import { api } from '../../api/client'
import { evidenceKeys } from '../../api/queryKeys'
import type { ChainVerification } from '../../api/types'

export function useChainVerification(id: string) {
  return useQuery({
    queryKey: evidenceKeys.verify(id),
    queryFn: async ({ signal }) => {
      const { data } = await api.get<ChainVerification>(`/evidence/${id}/chain/verify`, { signal })
      return data
    },
    enabled: false,
    retry: false,
  })
}
