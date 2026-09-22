import type { Custodian, CustodyEventType, TransferStatus } from '../api/types'

const dateTimeFormat = new Intl.DateTimeFormat('es-PE', {
  dateStyle: 'medium',
  timeStyle: 'short',
})

export function formatDateTime(iso: string): string {
  const date = new Date(/(Z|[+-]\d{2}:\d{2})$/.test(iso) ? iso : `${iso}Z`)
  return Number.isNaN(date.getTime()) ? iso : dateTimeFormat.format(date)
}

export const EVENT_TYPE_LABEL: Record<CustodyEventType, string> = {
  Created: 'Evidencia registrada',
  TransferRequested: 'Transferencia solicitada',
  TransferAccepted: 'Transferencia aceptada',
  TransferRejected: 'Transferencia rechazada',
}

export const TRANSFER_STATUS_LABEL: Record<TransferStatus, string> = {
  Pending: 'pendiente',
  Accepted: 'aceptada',
  Rejected: 'rechazada',
}

export function transferStatusLabel(status: string | undefined): string {
  return status && status in TRANSFER_STATUS_LABEL
    ? TRANSFER_STATUS_LABEL[status as TransferStatus]
    : (status ?? 'desconocido')
}

export function custodianOptionLabel(custodian: Custodian): string {
  return `${custodian.displayName} (${custodian.loginCode})`
}
