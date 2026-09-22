import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import './index.css'
import { AuthProvider } from './auth/AuthContext'
import { ProtectedRoute } from './auth/ProtectedRoute'
import { AppLayout } from './components/AppLayout'
import { NotFound } from './components/NotFound'
import { LoginPage } from './features/auth/LoginPage'
import { PendingInboxPage } from './features/custody-transfer/PendingInboxPage'
import { EvidenceDetailPage } from './features/evidence-detail/EvidenceDetailPage'
import { EvidenceListPage } from './features/evidence-list/EvidenceListPage'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: 1, refetchOnWindowFocus: false },
  },
})

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route element={<ProtectedRoute />}>
              <Route element={<AppLayout />}>
                <Route path="/evidence" element={<EvidenceListPage />} />
                <Route path="/evidence/:id" element={<EvidenceDetailPage />} />
                <Route path="/pending" element={<PendingInboxPage />} />
                <Route path="*" element={<NotFound />} />
              </Route>
            </Route>
            <Route path="/" element={<Navigate to="/evidence" replace />} />
          </Routes>
        </AuthProvider>
      </BrowserRouter>
    </QueryClientProvider>
  </StrictMode>,
)
