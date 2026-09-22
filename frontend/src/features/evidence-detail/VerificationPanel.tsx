import { toApiError } from '../../api/errors'
import type { ChainVerification } from '../../api/types'

interface VerificationPanelProps {
  result: ChainVerification | undefined
  error: unknown
  isVerifying: boolean
  onVerify: () => void
}

export function VerificationPanel({ result, error, isVerifying, onVerify }: VerificationPanelProps) {
  return (
    <div className="space-y-3">
      <button
        type="button"
        onClick={onVerify}
        disabled={isVerifying}
        className="rounded-md border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-800 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
      >
        {isVerifying ? 'Verificando…' : 'Verificar cadena'}
      </button>

      <div role="status" aria-live="polite">
        {error !== null && !isVerifying && (
          <p className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
            No se pudo verificar la cadena: {toApiError(error).message}
          </p>
        )}

        {result && !isVerifying && result.isValid && (
          <p className="rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-800">
            <span aria-hidden="true">✓</span> Cadena íntegra: todos los hashes coinciden.
          </p>
        )}

        {result && !isVerifying && !result.isValid && (
          <div className="rounded-md border border-red-300 bg-red-50 px-3 py-2 text-sm text-red-800">
            <p className="font-semibold">
              <span aria-hidden="true">✗</span> Cadena comprometida
            </p>
            {result.reason && <p className="mt-1">{result.reason}</p>}
            {result.firstInvalidEventId && (
              <p className="mt-1">
                Primer evento inválido: <span className="font-mono text-xs">{result.firstInvalidEventId}</span>{' '}
                (resaltado en la línea de tiempo).
              </p>
            )}
          </div>
        )}
      </div>
    </div>
  )
}
