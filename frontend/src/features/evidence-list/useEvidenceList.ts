import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { api } from '../../api/client'
import { evidenceKeys } from '../../api/queryKeys'
import type { EvidenceListParams, EvidencePage } from '../../api/types'

export function useEvidenceList(params: EvidenceListParams) {
  return useQuery({
    queryKey: evidenceKeys.list(params),
    queryFn: async ({ signal }) => {
      const { data } = await api.get<EvidencePage>('/evidence', {
        params: {
          search: params.search || undefined,
          custodianId: params.custodianId || undefined,
          cursor: params.cursor ?? undefined,
          sort: params.sort,
        },
        signal,
      })
      return data
    },
    placeholderData: keepPreviousData,
  })
}
