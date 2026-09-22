import { useMutation } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { api } from '../../api/client'
import { toApiError } from '../../api/errors'
import type { LoginRequest, LoginResponse } from '../../api/types'
import { useAuth } from '../../auth/AuthContext'

interface LocationState {
  from?: { pathname: string; search: string }
}

export function LoginPage() {
  const { token, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [loginCode, setLoginCode] = useState('')

  const mutation = useMutation({
    mutationFn: async (body: LoginRequest) => {
      const { data } = await api.post<LoginResponse>('/auth/login', body)
      return data
    },
    onSuccess: (data) => {
      login({ token: data.token, custodianId: data.custodianId, displayName: data.displayName, role: data.role })
      const from = (location.state as LocationState | null)?.from
      navigate(from ? `${from.pathname}${from.search}` : '/evidence', { replace: true })
    },
  })

  if (token && !mutation.isPending) return <Navigate to="/evidence" replace />

  const error = mutation.error ? toApiError(mutation.error) : null

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    const code = loginCode.trim()
    if (code) mutation.mutate({ loginCode: code })
  }

  return (
    <main className="flex min-h-screen items-center justify-center px-4">
      <form
        onSubmit={handleSubmit}
        className="w-full max-w-sm space-y-5 rounded-lg border border-slate-200 bg-white p-8 shadow-sm"
      >
        <div>
          <h1 className="text-xl font-semibold text-slate-900">Evidence Chain</h1>
          <p className="mt-1 text-sm text-slate-500">Ingresa con tu código de acceso.</p>
        </div>

        <div>
          <label htmlFor="loginCode" className="block text-sm font-medium text-slate-700">
            Código de acceso
          </label>
          <input
            id="loginCode"
            type="text"
            autoComplete="username"
            autoFocus
            value={loginCode}
            onChange={(e) => setLoginCode(e.target.value)}
            placeholder="laura.vega"
            className="mt-1 block w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none focus:ring-1 focus:ring-slate-500"
          />
        </div>

        {error && (
          <p role="alert" className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
            {error.message}
          </p>
        )}

        <button
          type="submit"
          disabled={mutation.isPending || !loginCode.trim()}
          className="w-full rounded-md bg-slate-900 px-4 py-2 text-sm font-medium text-white hover:bg-slate-700 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {mutation.isPending ? 'Ingresando…' : 'Ingresar'}
        </button>
      </form>
    </main>
  )
}
