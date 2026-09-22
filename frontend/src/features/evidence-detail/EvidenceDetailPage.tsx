import { useRef, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { isRetryable, toApiError } from '../../api/errors'
import type { Custodian } from '../../api/types'
import { useAuth } from '../../auth/AuthContext'
import { formatDateTime, transferStatusLabel } from '../../lib/format'
import { TransferModal } from '../custody-transfer/TransferModal'
import { useResolveTransfer } from '../custody-transfer/useResolveTransfer'
import { useTransferMutation } from '../custody-transfer/useTransferMutation'
import { AnomalyBanner } from './AnomalyBanner'
import { ChainTimeline } from './ChainTimeline'
import { PendingTransferBanner } from './PendingTransferBanner'
import { useChainVerification } from './useChainVerification'
import { useEvidenceChain } from './useEvidenceChain'
import { useEvidenceDetail } from './useEvidenceDetail'
import { VerificationPanel } from './VerificationPanel'

function BackLink() {
  const navigate = useNavigate()
  return (
    <Link
      to="/evidence"
      onClick={(event) => {
        // Si venimos de la bandeja, "volver" conserva sus filtros y su cursor.
        const idx = (window.history.state as { idx?: number } | null)?.idx ?? 0
        if (idx > 0) {
          event.preventDefault()
          navigate(-1)
        }
      }}
      className="text-sm text-slate-600 hover:text-slate-900 hover:underline"
    >
      ← Volver a la bandeja
    </Link>
  )
}

export function EvidenceDetailPage() {
  const { id = '' } = useParams<{ id: string }>()
  const { role, custodianId } = useAuth()

  const detail = useEvidenceDetail(id)
  const chain = useEvidenceChain(id)
  const verification = useChainVerification(id)
  const transfer = useTransferMutation(id)
  const resolve = useResolveTransfer(id)

  const [modalOpen, setModalOpen] = useState(false)
  const transferButtonRef = useRef<HTMLButtonElement>(null)

  if (detail.isPending) {
    return (
      <div className="space-y-4" aria-busy="true" aria-label="Cargando evidencia">
        <div className="h-6 w-40 animate-pulse rounded bg-slate-200" />
        <div className="h-28 animate-pulse rounded-lg bg-slate-200" />
        <div className="h-64 animate-pulse rounded-lg bg-slate-200" />
      </div>
    )
  }

  if (detail.isError) {
    const error = toApiError(detail.error)
    return (
      <div className="space-y-4">
        <BackLink />
        <div role="alert" className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
          <p className="font-medium">
            {error.status === 404 ? 'La evidencia no existe.' : 'No se pudo cargar la evidencia.'}
          </p>
          {error.status !== 404 && <p className="mt-1">{error.message}</p>}
          {error.status !== 404 && (
            <button
              type="button"
              onClick={() => void detail.refetch()}
              className="mt-3 rounded-md border border-red-300 bg-white px-3 py-1 text-red-700 hover:bg-red-100"
            >
              Reintentar
            </button>
          )}
        </div>
      </div>
    )
  }

  const evidence = detail.data
  const pending = evidence.pendingTransfer
  const isInvestigador = role === 'Investigador'
  // El detalle no expone el id del usuario en sesión, así que se compara por nombre visible;
  // el backend igualmente responde 403 si el destinatario no coincide.
  const isRecipient = role === 'Custodio' && pending !== null && pending.toCustodianId === custodianId
  const transferBlocked = pending !== null

  function openModal() {
    if (transferBlocked) return
    transfer.reset()
    resolve.reset()
    setModalOpen(true)
  }

  function handleConfirm(toCustodian: Custodian) {
    setModalOpen(false)
    transfer.submit(toCustodian)
  }

  const transferError = transfer.error
  const resolveError = resolve.error

  return (
    <div className="space-y-6">
      <BackLink />

      <section className="rounded-lg border border-slate-200 bg-white p-5">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <h1 className="font-mono text-xl font-semibold">{evidence.code}</h1>
            <p className="mt-1 text-slate-700">{evidence.description}</p>
          </div>
          {isInvestigador && (
            <button
              ref={transferButtonRef}
              type="button"
              // aria-disabled (y no disabled) para que el botón conserve el foco al cerrar el modal.
              aria-disabled={transferBlocked}
              title={transferBlocked ? 'Ya existe una transferencia pendiente para esta evidencia.' : undefined}
              onClick={openModal}
              className="rounded-md bg-slate-900 px-4 py-2 text-sm font-medium text-white hover:bg-slate-700 aria-disabled:cursor-not-allowed aria-disabled:opacity-50 aria-disabled:hover:bg-slate-900"
            >
              Transferir custodia
            </button>
          )}
        </div>
        <dl className="mt-4 grid gap-x-8 gap-y-2 text-sm sm:grid-cols-2">
          <div>
            <dt className="text-xs uppercase tracking-wide text-slate-500">Custodio actual</dt>
            <dd className="text-slate-900">{evidence.currentCustodianName}</dd>
          </div>
          <div>
            <dt className="text-xs uppercase tracking-wide text-slate-500">Registrada</dt>
            <dd className="text-slate-900">{formatDateTime(evidence.createdAtUtc)}</dd>
          </div>
        </dl>
      </section>

      {evidence.hasAnomaly && (
        <AnomalyBanner severity={evidence.anomalySeverity} reason={evidence.anomalyReason} />
      )}

      {pending && (
        <PendingTransferBanner
          transfer={pending}
          canResolve={isRecipient}
          pendingAction={resolve.pendingAction}
          onResolve={(action) => resolve.resolve(pending, action)}
        />
      )}

      {transferError && (
        <div role="alert" className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
          <p className="font-medium">No se pudo crear la transferencia. No se realizó ningún cambio.</p>
          <p className="mt-1">{transferError.message}</p>
          <div className="mt-3 flex gap-2">
            {isRetryable(transferError) && (
              <button
                type="button"
                onClick={transfer.retry}
                disabled={transfer.isPending}
                className="rounded-md border border-red-300 bg-white px-3 py-1 text-red-700 hover:bg-red-100 disabled:opacity-50"
              >
                Reintentar
              </button>
            )}
            <button type="button" onClick={transfer.reset} className="px-3 py-1 text-red-700 underline">
              Descartar
            </button>
          </div>
        </div>
      )}

      {resolveError && (
        <div role="alert" className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
          <p>{resolveError.message}</p>
          <button type="button" onClick={resolve.reset} className="mt-2 underline">
            Descartar
          </button>
        </div>
      )}

      {resolve.result && !resolveError && (
        <p role="status" className="rounded-lg border border-emerald-200 bg-emerald-50 p-4 text-sm text-emerald-800">
          Transferencia {transferStatusLabel(resolve.result.status)} correctamente.
        </p>
      )}

      <section className="rounded-lg border border-slate-200 bg-white p-5">
        <div className="mb-4 flex flex-wrap items-start justify-between gap-4">
          <h2 className="text-base font-semibold">Cadena de custodia</h2>
          <VerificationPanel
            result={verification.data}
            error={verification.error}
            isVerifying={verification.isFetching}
            onVerify={() => void verification.refetch()}
          />
        </div>

        {chain.isPending && (
          <div className="space-y-3" aria-busy="true" aria-label="Cargando cadena de custodia">
            {Array.from({ length: 3 }, (_, i) => (
              <div key={i} className="h-16 animate-pulse rounded-md bg-slate-200" />
            ))}
          </div>
        )}

        {chain.isError && (
          <div role="alert" className="text-sm text-red-700">
            No se pudo cargar la cadena de custodia: {toApiError(chain.error).message}{' '}
            <button type="button" onClick={() => void chain.refetch()} className="underline">
              Reintentar
            </button>
          </div>
        )}

        {chain.isSuccess && (
          <ChainTimeline events={chain.data} invalidEventId={verification.data?.firstInvalidEventId ?? null} />
        )}
      </section>

      {modalOpen && (
        <TransferModal
          evidenceCode={evidence.code}
          currentCustodianId={evidence.currentCustodianId}
          onConfirm={handleConfirm}
          onClose={() => setModalOpen(false)}
          returnFocusRef={transferButtonRef}
        />
      )}
    </div>
  )
}
