import { useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/client'
import { toApiError, type ApiError } from '../../api/errors'
import { evidenceKeys } from '../../api/queryKeys'
import type { CreateTransferRequest, Custodian, EvidenceDetail, TransferResponse } from '../../api/types'

interface TransferVariables {
  toCustodian: Custodian
  idempotencyKey: string
}

interface TransferContext {
  previous: EvidenceDetail | undefined
}

function describeTransferError(error: ApiError): string {
  switch (error.status) {
    case 403:
      return 'No tienes permiso para iniciar transferencias de custodia.'
    case 404:
      return 'La evidencia ya no existe.'
    default:
      return error.message
  }
}

export function useTransferMutation(evidenceId: string) {
  const queryClient = useQueryClient()
  const detailKey = evidenceKeys.detail(evidenceId)

  const mutation = useMutation<TransferResponse, unknown, TransferVariables, TransferContext>({
    mutationFn: async ({ toCustodian, idempotencyKey }) => {
      const body: CreateTransferRequest = { evidenceId, toCustodianId: toCustodian.id }
      const { data } = await api.post<TransferResponse>('/custody-transfers', body, {
        headers: { 'Idempotency-Key': idempotencyKey },
      })
      return data
    },

    // Optimista: la transferencia aparece como Pendiente antes de que responda el servidor.
    onMutate: async ({ toCustodian }) => {
      await queryClient.cancelQueries({ queryKey: detailKey })
      const previous = queryClient.getQueryData<EvidenceDetail>(detailKey)
      if (previous) {
        queryClient.setQueryData<EvidenceDetail>(detailKey, {
          ...previous,
          pendingTransfer: {
            id: `optimistic-${crypto.randomUUID()}`,
            toCustodianId: toCustodian.id,
            toCustodianName: toCustodian.displayName,
            requestedAtUtc: new Date().toISOString(),
            eTag: '',
            isOptimistic: true,
          },
        })
      }
      return { previous }
    },

    // Rollback: si el servidor no la aceptó, nunca debe quedar visualmente "confirmada".
    onError: (_error, _variables, context) => {
      if (context?.previous) queryClient.setQueryData(detailKey, context.previous)
    },

    onSettled: async () => {
      queryClient.resetQueries({ queryKey: evidenceKeys.verify(evidenceId) })
      await queryClient.invalidateQueries({ queryKey: evidenceKeys.all })
    },
  })

  return {
    // Nueva intención de transferencia: genera una Idempotency-Key nueva
    submit: (toCustodian: Custodian) =>
      mutation.mutate({ toCustodian, idempotencyKey: crypto.randomUUID() }),
    // Reintento de la MISMA petición (misma Idempotency-Key)
    retry: () => {
      if (mutation.variables) mutation.mutate(mutation.variables)
    },
    reset: mutation.reset,
    isPending: mutation.isPending,
    error: mutation.error ? describeApiError(mutation.error) : null,
  }
}

function describeApiError(error: unknown): ApiError {
  const apiError = toApiError(error)
  return { ...apiError, message: describeTransferError(apiError) }
}
