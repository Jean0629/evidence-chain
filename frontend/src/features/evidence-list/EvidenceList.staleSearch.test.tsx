import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { fireEvent, render, screen } from '@testing-library/react'
import { HttpResponse, http } from 'msw'
import { setupServer } from 'msw/node'
import { act } from 'react'
import { MemoryRouter } from 'react-router-dom'
import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest'
import type { EvidenceListItem } from '../../api/types'
import { EvidenceListPage } from './EvidenceListPage'

const DEBOUNCE_WAIT_MS = 400

function staleItem(code: string): EvidenceListItem {
  return {
    id: `id-${code}`,
    code,
    description: 'Evidencia de prueba',
    currentCustodianName: 'Jane Doe',
    lastEventAtUtc: '2026-01-01T00:00:00Z',
    hasAnomaly: false,
    isIntegrityValid: true,
  }
}

const server = setupServer(
  http.get('*/custodians', () => HttpResponse.json([])),
)

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
afterEach(() => server.resetHandlers())
afterAll(() => server.close())

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/evidence']}>
        <EvidenceListPage />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

async function wait(ms: number) {
  await act(async () => {
    await new Promise((resolve) => setTimeout(resolve, ms))
  })
}

describe('EvidenceListPage — respuesta de búsqueda obsoleta', () => {
  it('no reemplaza el resultado del término de búsqueda más reciente', async () => {
    let releaseStaleResponse!: () => void
    const staleGate = new Promise<void>((resolve) => {
      releaseStaleResponse = resolve
    })

    server.use(
      http.get('*/evidence', async ({ request }) => {
        const search = new URL(request.url).searchParams.get('search')

        if (search === 'e') {
          await staleGate
          return HttpResponse.json({ items: [staleItem('EV-STALE')], nextCursor: null })
        }
        if (search === 'evi') {
          return HttpResponse.json({ items: [staleItem('EV-FRESH')], nextCursor: null })
        }
        // Carga inicial sin filtro.
        return HttpResponse.json({ items: [], nextCursor: null })
      }),
    )

    renderPage()

    // Carga inicial (sin búsqueda) resuelta.
    await screen.findByText('No se encontraron evidencias con estos filtros.')

    const searchInput = screen.getByRole('searchbox', { name: 'Buscar' })

    // 1) El usuario escribe "e" y espera lo suficiente para que el debounce dispare
    //    GET /evidence?search=e (que se queda pendiente, ver staleGate arriba).
    fireEvent.change(searchInput, { target: { value: 'e' } })
    await wait(DEBOUNCE_WAIT_MS)

    // 2) Luego escribe "evi": nuevo debounce, nueva queryKey, nueva petición —
    //    esta responde de inmediato.
    fireEvent.change(searchInput, { target: { value: 'evi' } })
    await wait(DEBOUNCE_WAIT_MS)

    // La búsqueda vigente ("evi") ya se muestra en pantalla.
    expect(await screen.findByText('EV-FRESH')).toBeInTheDocument()
    expect(screen.queryByText('EV-STALE')).not.toBeInTheDocument()

    // 3) Recién ahora se libera la respuesta tardía de "e".
    await act(async () => {
      releaseStaleResponse()
      await Promise.resolve()
    })
    // Le da tiempo a la promesa liberada a resolverse y, si el bug existiera, a pisar
    // el resultado visible.
    await wait(50)

    // La respuesta obsoleta actualiza únicamente la cache de la queryKey "search=e";
    // la bandeja sigue leyendo la queryKey "search=evi" y nunca debe mostrar EV-STALE.
    expect(screen.getByText('EV-FRESH')).toBeInTheDocument()
    expect(screen.queryByText('EV-STALE')).not.toBeInTheDocument()
  }, 10_000)
})
