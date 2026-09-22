import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { api } from '../../api/client'
import { evidenceKeys } from '../../api/queryKeys'
import type { EvidenceListParams, EvidencePage } from '../../api/types'

export function useEvidenceList(params: EvidenceListParams) {
  return useQuery({
    // Cada combinación de filtros/cursor/orden es una key distinta: una respuesta tardía
    // de una búsqueda vieja se guarda en su propia entrada y nunca pisa la vigente.
    queryKey: evidenceKeys.list(params),
    // `signal` cancela la petición HTTP cuando la key deja de estar observada.
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
