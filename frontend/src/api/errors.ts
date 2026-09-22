import axios from 'axios'
import type { ProblemDetails } from './types'

export interface ApiError {
  status?: number
  problem?: ProblemDetails
  message: string
}

export function toApiError(error: unknown): ApiError {
  if (axios.isAxiosError<ProblemDetails>(error)) {
    if (!error.response) {
      return { message: 'No se pudo conectar con el servidor. Revisa tu conexión e inténtalo de nuevo.' }
    }
    const { status, data } = error.response
    const problem = typeof data === 'object' && data !== null ? data : undefined
    return { status, problem, message: problem?.title ?? `El servidor respondió con un error (${status}).` }
  }
  return { message: error instanceof Error ? error.message : 'Ocurrió un error inesperado.' }
}

export function isRetryable(error: ApiError): boolean {
  return error.status === undefined || error.status >= 500
}
