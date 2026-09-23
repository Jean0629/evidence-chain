import { useEffect, useRef, type RefObject } from 'react'
import type { CustodyEvent, CustodyEventType, PendingTransfer } from '../../api/types'
import { EVENT_TYPE_LABEL, formatDateTime } from '../../lib/format'
import type { ResolveAction } from '../custody-transfer/useResolveTransfer'

const DOT_COLOR: Record<CustodyEventType, string> = {
  Created: 'bg-slate-500',
  TransferRequested: 'bg-amber-500',
  TransferAccepted: 'bg-emerald-500',
  TransferRejected: 'bg-red-500',
}

function shortHash(hash: string): string {
  return hash.length > 16 ? `${hash.slice(0, 8)}…${hash.slice(-8)}` : hash
}

function PendingBadge() {
  return (
    <span className="ml-2 rounded-full bg-sky-100 px-2 py-0.5 text-xs font-medium text-sky-800">Pendiente</span>
  )
}

function ResolveActions({
  pendingAction,
  onResolve,
}: {
  pendingAction: ResolveAction | undefined
  onResolve: (action: ResolveAction) => void
}) {
  const busy = pendingAction !== undefined
  return (
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
  )
}

interface ChainTimelineProps {
  events: CustodyEvent[]
  invalidEventId: string | null

  pendingTransfer: PendingTransfer | null
  canResolvePending: boolean
  pendingAction: ResolveAction | undefined
  onResolve: (action: ResolveAction) => void

  canStartTransfer: boolean
  onStartTransfer: () => void
  startTransferRef: RefObject<HTMLButtonElement | null>
}

export function ChainTimeline({
  events,
  invalidEventId,
  pendingTransfer,
  canResolvePending,
  pendingAction,
  onResolve,
  canStartTransfer,
  onStartTransfer,
  startTransferRef,
}: ChainTimelineProps) {
  const invalidRef = useRef<HTMLLIElement>(null)
  const lastPointRef = useRef<HTMLLIElement>(null)
  const hasAutoScrolledRef = useRef(false)

  // "Verificar cadena" encontró un evento inválido: se resalta y centra.
  useEffect(() => {
    if (invalidEventId) invalidRef.current?.scrollIntoView({ block: 'center', behavior: 'smooth' })
  }, [invalidEventId])

  useEffect(() => {
    if (hasAutoScrolledRef.current || !lastPointRef.current) return
    hasAutoScrolledRef.current = true
    lastPointRef.current.scrollIntoView({ block: 'center', behavior: 'smooth' })
  })

  if (events.length === 0 && !canStartTransfer && pendingTransfer === null) {
    return <p className="text-sm text-slate-500">Esta evidencia aún no tiene eventos de custodia.</p>
  }

  const lastEvent = events[events.length - 1] as CustodyEvent | undefined
  const pendingMatchesLastEvent =
    pendingTransfer !== null && !pendingTransfer.isOptimistic && lastEvent?.type === 'TransferRequested'
  const showSyntheticPendingRow = pendingTransfer !== null && !pendingMatchesLastEvent

  return (
    <ol className="relative ml-2 border-l-2 border-slate-200">
      {events.map((event, index) => {
        const isInvalid = event.id === invalidEventId
        const isLastRealEvent = index === events.length - 1
        const isLastPoint = isLastRealEvent && !showSyntheticPendingRow && !canStartTransfer
        const isPendingRow = pendingMatchesLastEvent && isLastRealEvent

        return (
          <li
            key={event.id}
            ref={(el) => {
              if (isInvalid) invalidRef.current = el
              if (isLastPoint) lastPointRef.current = el
            }}
            aria-current={isInvalid ? 'true' : undefined}
            className={`relative mb-4 ml-6 scroll-mt-20 rounded-md border p-3 last:mb-0 ${
              isInvalid ? 'border-red-400 bg-red-50 ring-2 ring-red-300' : 'border-slate-200 bg-white'
            }`}
          >
            <span
              aria-hidden="true"
              className={`absolute -left-[33px] top-4 h-3 w-3 rounded-full ring-4 ring-slate-50 ${
                isInvalid ? 'bg-red-600' : DOT_COLOR[event.type]
              }`}
            />
            <div className="flex flex-wrap items-baseline justify-between gap-x-3">
              <p className="text-sm font-medium text-slate-900">
                {EVENT_TYPE_LABEL[event.type] ?? event.type}
                {isInvalid && (
                  <span className="ml-2 rounded bg-red-600 px-1.5 py-0.5 text-xs font-semibold text-white">
                    Primer evento inválido
                  </span>
                )}
                {isPendingRow && <PendingBadge />}
              </p>
              <time dateTime={event.occurredAtUtc} className="text-xs text-slate-500">
                {formatDateTime(event.occurredAtUtc)}
              </time>
            </div>
            {event.type === 'TransferRequested' ? (
              <p className="text-sm text-slate-600">
                por <span className="font-medium text-slate-800">{event.actorName}</span>
                {event.fromCustodianName && (
                  <>
                    {' '}
                    <span aria-hidden="true">→</span> remitente:{' '}
                    <span className="font-medium text-slate-800">{event.fromCustodianName}</span>
                  </>
                )}
                {event.toCustodianName && (
                  <>
                    {' '}
                    <span aria-hidden="true">→</span> destinatario:{' '}
                    <span className="font-medium text-slate-800">{event.toCustodianName}</span>
                  </>
                )}
              </p>
            ) : (
              <p className="text-sm text-slate-600">Por {event.actorName}</p>
            )}
            <dl className="mt-2 grid grid-cols-[auto_1fr] gap-x-2 gap-y-0.5 font-mono text-xs text-slate-500">
              <dt>hash</dt>
              <dd title={event.hash}>{shortHash(event.hash)}</dd>
              <dt>prev</dt>
              <dd title={event.previousHash}>{shortHash(event.previousHash)}</dd>
            </dl>

            {isPendingRow && canResolvePending && (
              <ResolveActions pendingAction={pendingAction} onResolve={onResolve} />
            )}
          </li>
        )
      })}

      {showSyntheticPendingRow && pendingTransfer && (
        <li
          ref={lastPointRef}
          className="relative mb-4 ml-6 scroll-mt-20 rounded-md border border-sky-200 bg-sky-50 p-3 last:mb-0"
        >
          <span
            aria-hidden="true"
            className="absolute -left-[33px] top-4 h-3 w-3 rounded-full bg-amber-500 ring-4 ring-slate-50"
          />
          <div className="flex flex-wrap items-baseline justify-between gap-x-3">
            <p className="text-sm font-medium text-slate-900">
              Transferencia solicitada
              <PendingBadge />
              {pendingTransfer.isOptimistic && (
                <span className="ml-2 text-xs font-normal text-sky-700">(enviando…)</span>
              )}
            </p>
            <time dateTime={pendingTransfer.requestedAtUtc} className="text-xs text-slate-500">
              {formatDateTime(pendingTransfer.requestedAtUtc)}
            </time>
          </div>
          <p className="text-sm text-slate-600">
            destinatario: <span className="font-medium text-slate-800">{pendingTransfer.toCustodianName}</span>
          </p>

          {canResolvePending && !pendingTransfer.isOptimistic && (
            <ResolveActions pendingAction={pendingAction} onResolve={onResolve} />
          )}
        </li>
      )}

      {canStartTransfer && (
        <li
          ref={lastPointRef}
          className="relative ml-6 scroll-mt-20 rounded-md border border-dashed border-slate-300 bg-slate-50 p-3"
        >
          <span
            aria-hidden="true"
            className="absolute -left-[33px] top-4 h-3 w-3 rounded-full bg-slate-300 ring-4 ring-slate-50"
          />
          <button
            ref={startTransferRef}
            type="button"
            onClick={onStartTransfer}
            className="mt-2 rounded-md bg-slate-900 px-4 py-2 text-sm font-medium text-white hover:bg-slate-700"
          >
            Transferir custodia
          </button>
        </li>
      )}
    </ol>
  )
}
