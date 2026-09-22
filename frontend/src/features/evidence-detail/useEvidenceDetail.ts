import { useQuery } from '@tanstack/react-query'
import { api } from '../../api/client'
import { toApiError } from '../../api/errors'
import { evidenceKeys } from '../../api/queryKeys'
import type { EvidenceDetail } from '../../api/types'

export function useEvidenceDetail(id: string) {
  return useQuery({
    queryKey: evidenceKeys.detail(id),
    queryFn: async ({ signal }) => {
      const { data } = await api.get<EvidenceDetail>(`/evidence/${id}`, { signal })
      return data
    },
    retry: (failureCount, error) => toApiError(error).status !== 404 && failureCount < 1,
  })
}
