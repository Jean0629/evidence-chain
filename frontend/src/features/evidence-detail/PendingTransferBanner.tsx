import type { PendingTransfer } from '../../api/types'
import type { ResolveAction } from '../custody-transfer/useResolveTransfer'
import { formatDateTime } from '../../lib/format'

interface PendingTransferBannerProps {
  transfer: PendingTransfer
  canResolve: boolean
  pendingAction: ResolveAction | undefined
  onResolve: (action: ResolveAction) => void
}

export function PendingTransferBanner({ transfer, canResolve, pendingAction, onResolve }: PendingTransferBannerProps) {
  const busy = pendingAction !== undefined

  return (
    <div className="rounded-lg border border-sky-200 bg-sky-50 p-4 text-sky-900">
      <p className="text-sm font-semibold">
        Transferencia pendiente
        {transfer.isOptimistic && <span className="ml-2 font-normal text-sky-700">(enviando…)</span>}
      </p>
      <p className="mt-1 text-sm">
        La custodia se transferirá a <span className="font-medium">{transfer.toCustodianName}</span>, quien debe
        aceptarla. Solicitada el {formatDateTime(transfer.requestedAtUtc)}.
      </p>

      {canResolve && !transfer.isOptimistic && (
        <div className="mt-3 flex gap-2">
          <button
            type="button"
            disabled={busy}
            onClick={() => onResolve('accept')}
            className="rounded-md bg-emerald-600 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {pendingAction === 'accept' ? 'Aceptando…' : 'Aceptar'}
          </button>
          <button
            type="button"
            disabled={busy}
            onClick={() => onResolve('reject')}
            className="rounded-md border border-red-300 bg-white px-4 py-2 text-sm font-medium text-red-700 hover:bg-red-50 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {pendingAction === 'reject' ? 'Rechazando…' : 'Rechazar'}
          </button>
        </div>
      )}
    </div>
  )
}
