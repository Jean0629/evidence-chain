import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { HttpResponse, http } from 'msw'
import { setupServer } from 'msw/node'
import { act } from 'react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it } from 'vitest'
import type { EvidenceDetail } from '../../api/types'
import { AuthProvider } from '../../auth/AuthContext'
import { clearSession, writeSession } from '../../auth/storage'
import { EvidenceDetailPage } from '../evidence-detail/EvidenceDetailPage'

const EVIDENCE_ID = 'ev-1'
const TRANSFER_ID = 'tr-1'
const CUSTODIAN_ID = 'custodio-1'

function evidenceDetail(): EvidenceDetail {
  return {
    id: EVIDENCE_ID,
    code: 'EV-0001',
    description: 'Evidencia de prueba',
    currentCustodianId: 'from-1',
    currentCustodianName: 'Jane Doe',
    createdAtUtc: '2026-01-01T00:00:00Z',
    hasAnomaly: false,
    anomalySeverity: null,
    anomalyReason: null,
    pendingTransfer: {
      id: TRANSFER_ID,
      toCustodianId: CUSTODIAN_ID,
      toCustodianName: 'Richard Roe',
      requestedAtUtc: '2026-01-02T00:00:00Z',
      eTag: '"AAAAAAAAAAA="',
    },
  }
}

let detailRequestCount = 0

const server = setupServer(
  http.get('*/evidence/:id', () => {
    detailRequestCount += 1
    return HttpResponse.json(evidenceDetail())
  }),
  http.get('*/evidence/:id/chain', () => HttpResponse.json([])),
)

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
beforeEach(() => {
  detailRequestCount = 0
})
afterEach(() => {
  server.resetHandlers()
  clearSession()
})
afterAll(() => server.close())

function renderDetail() {
  writeSession({ token: 'test-token', custodianId: CUSTODIAN_ID, displayName: 'Richard Roe', role: 'Custodio' })

  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })

  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/evidence/${EVIDENCE_ID}`]}>
        <AuthProvider>
          <Routes>
            <Route path="/evidence/:id" element={<EvidenceDetailPage />} />
          </Routes>
        </AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('useResolveTransfer — rollback y reconciliación ante 409', () => {
  it('revierte el estado "procesando" y muestra el error cuando el servidor responde 409', async () => {
    let releaseConflict!: () => void
    const conflictGate = new Promise<void>((resolve) => {
      releaseConflict = resolve
    })

    server.use(
      http.post(`*/custody-transfers/${TRANSFER_ID}/accept`, async () => {
        await conflictGate
        return HttpResponse.json(
          {
            type: 'concurrency-conflict',
            title: 'La transferencia fue modificada por otro usuario.',
            status: 409,
            currentStatus: 'Rejected',
          },
          { status: 409 },
        )
      }),
    )

    renderDetail()

    await screen.findByRole('button', { name: 'Aceptar' })
    expect(detailRequestCount).toBe(1)

    fireEvent.click(screen.getByRole('button', { name: 'Aceptar' }))

    const processingButton = await screen.findByRole('button', { name: 'Aceptando…' })
    expect(processingButton).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Rechazar' })).toBeDisabled()

    await act(async () => {
      releaseConflict()
      await Promise.resolve()
    })

    await waitFor(() => expect(screen.getByRole('button', { name: 'Aceptar' })).not.toBeDisabled())
    expect(screen.queryByRole('button', { name: 'Aceptando…' })).not.toBeInTheDocument()

    expect(
      await screen.findByText(/Esta transferencia ya fue rechazada por otro usuario/i),
    ).toBeInTheDocument()

    expect(screen.queryByText(/correctamente/)).not.toBeInTheDocument()

    await waitFor(() => expect(detailRequestCount).toBeGreaterThan(1))
  })
})
