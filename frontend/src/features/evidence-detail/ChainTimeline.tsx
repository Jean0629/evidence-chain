import { useEffect, useRef } from 'react'
import type { CustodyEvent, CustodyEventType } from '../../api/types'
import { EVENT_TYPE_LABEL, formatDateTime } from '../../lib/format'

const DOT_COLOR: Record<CustodyEventType, string> = {
  Created: 'bg-slate-500',
  TransferRequested: 'bg-amber-500',
  TransferAccepted: 'bg-emerald-500',
  TransferRejected: 'bg-red-500',
}

function shortHash(hash: string): string {
  return hash.length > 16 ? `${hash.slice(0, 8)}…${hash.slice(-8)}` : hash
}

interface ChainTimelineProps {
  events: CustodyEvent[]
  invalidEventId: string | null
}

export function ChainTimeline({ events, invalidEventId }: ChainTimelineProps) {
  const invalidRef = useRef<HTMLLIElement>(null)

  useEffect(() => {
    if (invalidEventId) invalidRef.current?.scrollIntoView({ block: 'center', behavior: 'smooth' })
  }, [invalidEventId])

  if (events.length === 0) {
    return <p className="text-sm text-slate-500">Esta evidencia aún no tiene eventos de custodia.</p>
  }

  return (
    <ol className="relative ml-2 border-l-2 border-slate-200">
      {events.map((event) => {
        const isInvalid = event.id === invalidEventId
        return (
          <li
            key={event.id}
            ref={isInvalid ? invalidRef : undefined}
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
          </li>
        )
      })}
    </ol>
  )
}
