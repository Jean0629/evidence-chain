import type { EvidenceListParams } from './types'

export const evidenceKeys = {
  all: ['evidence'] as const,
  list: (params: EvidenceListParams) => ['evidence', 'list', params] as const,
  detail: (id: string) => ['evidence', 'detail', id] as const,
  chain: (id: string) => ['evidence', 'chain', id] as const,
  verify: (id: string) => ['evidence', 'verify', id] as const,
}

export const transferKeys = {
  pending: ['transfers', 'pending'] as const,
}

export const custodianKeys = {
  all: ['custodians'] as const,
}
