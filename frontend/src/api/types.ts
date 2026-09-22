export type Role = 'Investigador' | 'Custodio' | 'Supervisor'

export interface LoginRequest {
  loginCode: string
}

export interface LoginResponse {
  token: string
  custodianId: string
  displayName: string
  role: Role
}

export type SortOrder = 'asc' | 'desc'

export interface EvidenceListItem {
  id: string
  code: string
  description: string
  currentCustodianName: string
  lastEventAtUtc: string
  hasAnomaly: boolean
  isIntegrityValid: boolean
}

export interface EvidencePage {
  items: EvidenceListItem[]
  nextCursor: string | null
}

export interface EvidenceListParams {
  search: string
  custodianId: string
  cursor: string | null
  sort: SortOrder
}

export type AnomalySeverity = 'low' | 'medium' | 'high'

export type TransferStatus = 'Pending' | 'Accepted' | 'Rejected'

export interface PendingTransfer {
  id: string
  toCustodianId: string
  toCustodianName: string
  requestedAtUtc: string
  eTag: string
  isOptimistic?: boolean
}

export interface EvidenceDetail {
  id: string
  code: string
  description: string
  currentCustodianId: string
  currentCustodianName: string
  createdAtUtc: string
  hasAnomaly: boolean
  anomalySeverity: AnomalySeverity | null
  anomalyReason: string | null
  pendingTransfer: PendingTransfer | null
}

export type CustodyEventType =
  | 'Created'
  | 'TransferRequested'
  | 'TransferAccepted'
  | 'TransferRejected'

export interface CustodyEvent {
  id: string
  type: CustodyEventType
  actorName: string
  toCustodianName?: string | null
  occurredAtUtc: string
  hash: string
  previousHash: string
}

export interface ChainVerification {
  isValid: boolean
  firstInvalidEventId: string | null
  reason: string | null
}

export interface Custodian {
  id: string
  displayName: string
  role: Role
  loginCode: string
}

export interface CreateTransferRequest {
  evidenceId: string
  toCustodianId: string
}

export interface TransferResponse {
  id: string
  evidenceId: string
  status: TransferStatus
  fromCustodianId: string
  toCustodianId: string
  requestedAtUtc: string
  eTag: string
}

export interface MyPendingTransfer {
  id: string
  evidenceId: string
  evidenceCode: string
  fromCustodianName: string
  requestedAtUtc: string
  eTag: string
  isIntegrityValid: boolean
}

export interface ProblemDetails {
  type?: 'concurrency-conflict' | 'invalid-transition'
  title?: string
  status?: number
  currentStatus?: string
}
