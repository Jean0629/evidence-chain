import type { AnomalySeverity } from '../../api/types'

const SEVERITY_STYLE: Record<AnomalySeverity, { label: string; className: string }> = {
  low: { label: 'Severidad baja', className: 'border-yellow-300 bg-yellow-50 text-yellow-900' },
  medium: { label: 'Severidad media', className: 'border-orange-300 bg-orange-50 text-orange-900' },
  high: { label: 'Severidad alta', className: 'border-red-300 bg-red-50 text-red-900' },
}

interface AnomalyBannerProps {
  severity: AnomalySeverity | null
  reason: string | null
}

export function AnomalyBanner({ severity, reason }: AnomalyBannerProps) {
  const style = SEVERITY_STYLE[severity ?? 'low']

  return (
    <div role="alert" className={`rounded-lg border p-4 ${style.className}`}>
      <p className="flex items-center gap-2 text-sm font-semibold">
        <span aria-hidden="true">⚠</span>
        Anomalía detectada · {style.label}
      </p>
      {reason && <p className="mt-1 text-sm">{reason}</p>}
    </div>
  )
}
