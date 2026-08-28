import { createContext, ReactNode, useContext, useMemo, useState } from 'react'
import { getAccessToken, setAccessToken } from '../services/apiClient'

const CLAIM_NAME = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'
const CLAIM_ROLE = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

type TokenClaims = {
  sub?: string
  shelter_id?: string
  [CLAIM_NAME]?: string
  [CLAIM_ROLE]?: string | string[]
}

type SessionState = {
  isAuthenticated: boolean
  userId: string | null
  userName: string | null
  roles: string[]
  shelterId: string | null
  login: (accessToken: string) => void
  logout: () => void
}

const SessionContext = createContext<SessionState | null>(null)

function decodeToken(token: string): TokenClaims | null {
  try {
    const payload = token.split('.')[1]
    const normalized = payload.replace(/-/g, '+').replace(/_/g, '/')
    const json = decodeURIComponent(
      atob(normalized)
        .split('')
        .map((char) => `%${char.charCodeAt(0).toString(16).padStart(2, '0')}`)
        .join(''),
    )
    return JSON.parse(json) as TokenClaims
  } catch {
    return null
  }
}

export function SessionProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => getAccessToken())

  const value = useMemo<SessionState>(() => {
    const claims = token ? decodeToken(token) : null
    const roleClaim = claims?.[CLAIM_ROLE]
    const roles = Array.isArray(roleClaim) ? roleClaim : roleClaim ? [roleClaim] : []
    return {
      isAuthenticated: Boolean(token),
      userId: claims?.sub ?? null,
      userName: claims?.[CLAIM_NAME] ?? null,
      roles,
      shelterId: claims?.shelter_id ?? null,
      login: (accessToken: string) => {
        setAccessToken(accessToken)
        setToken(accessToken)
      },
      logout: () => {
        setAccessToken(null)
        setToken(null)
      },
    }
  }, [token])

  return <SessionContext.Provider value={value}>{children}</SessionContext.Provider>
}

export function useSession(): SessionState {
  const context = useContext(SessionContext)
  if (!context) throw new Error('useSession debe usarse dentro de SessionProvider')
  return context
}
