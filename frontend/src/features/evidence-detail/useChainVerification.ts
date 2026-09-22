import { useQuery } from '@tanstack/react-query'
import { api } from '../../api/client'
import { evidenceKeys } from '../../api/queryKeys'
import type { ChainVerification } from '../../api/types'

/**
 * Verificación bajo demanda: la query está deshabilitada y se dispara con `refetch()`
 * desde el botón "Verificar cadena". Los mutations de transferencia reinician esta
 * entrada de la cache (resetQueries), porque un resultado viejo no vale para una cadena que cambió.
 */
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
