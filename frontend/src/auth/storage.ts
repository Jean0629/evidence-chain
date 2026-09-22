import type { Role } from '../api/types'

const TOKEN_KEY = 'token'
const CUSTODIAN_ID_KEY = 'custodianId'
const DISPLAY_NAME_KEY = 'displayName'
const ROLE_KEY = 'role'

export interface Session {
  token: string
  custodianId: string
  displayName: string
  role: Role
}

const ROLES: readonly string[] = ['Investigador', 'Custodio', 'Supervisor']

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY)
}

export function readSession(): Session | null {
  const token = localStorage.getItem(TOKEN_KEY)
  const custodianId = localStorage.getItem(CUSTODIAN_ID_KEY)
  const displayName = localStorage.getItem(DISPLAY_NAME_KEY)
  const role = localStorage.getItem(ROLE_KEY)
  if (!token || !custodianId || !displayName || !role || !ROLES.includes(role)) return null
  return { token, custodianId, displayName, role: role as Role }
}

export function writeSession(session: Session): void {
  localStorage.setItem(TOKEN_KEY, session.token)
  localStorage.setItem(CUSTODIAN_ID_KEY, session.custodianId)
  localStorage.setItem(DISPLAY_NAME_KEY, session.displayName)
  localStorage.setItem(ROLE_KEY, session.role)
}

export function clearSession(): void {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(CUSTODIAN_ID_KEY)
  localStorage.removeItem(DISPLAY_NAME_KEY)
  localStorage.removeItem(ROLE_KEY)
}
