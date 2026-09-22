import { useQuery } from '@tanstack/react-query'
import { api } from './client'
import { custodianKeys } from './queryKeys'
import type { Custodian } from './types'

export function useCustodians() {
  return useQuery({
    queryKey: custodianKeys.all,
    queryFn: async ({ signal }) => {
      const { data } = await api.get<Custodian[]>('/custodians', { signal })
      return data
    },
    staleTime: 5 * 60 * 1000,
  })
}
