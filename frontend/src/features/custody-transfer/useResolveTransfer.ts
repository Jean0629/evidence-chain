import { useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/client'
import { toApiError, type ApiError } from '../../api/errors'
import { evidenceKeys, transferKeys } from '../../api/queryKeys'
import type { PendingTransfer, TransferResponse } from '../../api/types'
import { transferStatusLabel } from '../../lib/format'

export type ResolveAction = 'accept' | 'reject'

interface ResolveVariables {
  transfer: PendingTransfer
  action: ResolveAction
}

const ACTION_VERB: Record<ResolveAction, string> = {
  accept: 'aceptar',
  reject: 'rechazar',
}

function describeResolveError(error: ApiError, action: ResolveAction): string {
  if (error.status === 409 && error.problem?.type === 'concurrency-conflict') {
    return `Esta transferencia ya fue ${transferStatusLabel(error.problem.currentStatus)} por otro usuario. Actualiza la página para ver el estado real.`
  }
  if (error.status === 409 && error.problem?.type === 'invalid-transition') {
    return `No se puede ${ACTION_VERB[action]} la transferencia: ya está ${transferStatusLabel(error.problem.currentStatus)}.`
  }
  if (error.status === 403) return 'No tienes permiso para resolver esta transferencia.'
  if (error.status === 404) return 'La transferencia ya no existe.'
  return error.message
}

export function useResolveTransfer(evidenceId: string) {
  const queryClient = useQueryClient()

  const mutation = useMutation<TransferResponse, unknown, ResolveVariables>({
    mutationFn: async ({ transfer, action }) => {
      // Concurrencia optimista: If-Match lleva el ETag con el que se vio la transferencia.
      const { data } = await api.post<TransferResponse>(
        `/custody-transfers/${transfer.id}/${action}`,
        undefined,
        { headers: { 'If-Match': transfer.eTag } },
      )
      return data
    },

    // Tanto en éxito como en error (409 incluido) se recargan los datos para mostrar el estado real.
    onSettled: async () => {
      queryClient.resetQueries({ queryKey: evidenceKeys.verify(evidenceId) })
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: evidenceKeys.all }),
        // La transferencia resuelta debe salir de la bandeja de pendientes y del contador.
        queryClient.invalidateQueries({ queryKey: transferKeys.pending }),
      ])
    },
  })

  return {
    resolve: (transfer: PendingTransfer, action: ResolveAction) => mutation.mutate({ transfer, action }),
    reset: mutation.reset,
    isPending: mutation.isPending,
    pendingAction: mutation.isPending ? mutation.variables?.action : undefined,
    result: mutation.data,
    error: mutation.error
      ? (() => {
          const apiError = toApiError(mutation.error)
          const action = mutation.variables?.action ?? 'accept'
          return { ...apiError, message: describeResolveError(apiError, action) }
        })()
      : null,
  }
}
