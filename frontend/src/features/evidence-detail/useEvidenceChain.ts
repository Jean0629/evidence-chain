import { useQuery } from '@tanstack/react-query'
import { api } from '../../api/client'
import { evidenceKeys } from '../../api/queryKeys'
import type { CustodyEvent } from '../../api/types'

export function useEvidenceChain(id: string) {
  return useQuery({
    queryKey: evidenceKeys.chain(id),
    queryFn: async ({ signal }) => {
      const { data } = await api.get<CustodyEvent[]>(`/evidence/${id}/chain`, { signal })
      return data
    },
  })
}
