import { createContext, ReactNode, useContext, useEffect, useMemo, useState } from 'react'
import {
  ensureFreshToken,
  getAccessToken,
  logoutRequest,
  setAccessToken,
  setRefreshToken,
  TOKEN_REFRESHED_EVENT,
} from '../services/apiClient'

const CLAIM_NAME = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'
const CLAIM_ROLE = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

type TokenClaims = {
  sub?: string
  shelter_id?: string
  exp?: number
  [CLAIM_NAME]?: string
  [CLAIM_ROLE]?: string | string[]
}

type SessionState = {
  isAuthenticated: boolean
  userId: string | null
  userName: string | null
  roles: string[]
  shelterId: string | null
  login: (accessToken: string, refreshToken: string) => void
  logout: () => Promise<void>
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

  useEffect(() => {
    function onTokenRefreshed(event: Event) {
      setToken((event as CustomEvent<string>).detail)
    }
    window.addEventListener(TOKEN_REFRESHED_EVENT, onTokenRefreshed)
    return () => window.removeEventListener(TOKEN_REFRESHED_EVENT, onTokenRefreshed)
  }, [])

  useEffect(() => {
    if (!token) return
    const claims = decodeToken(token)
    if (!claims?.exp) return

    // Proactively renew ~5 minutes before expiry so an active session never has to wait for a 401
    // to trigger the reactive refresh path — the user simply never sees an interruption.
    const msUntilRefresh = claims.exp * 1000 - Date.now() - 5 * 60 * 1000
    const timer = window.setTimeout(
      () => {
        ensureFreshToken()
          .then((auth) => setToken(auth.accessToken))
          .catch(() => {
            // A failed proactive refresh is not fatal here — the next request's reactive 401 path
            // (or the user's own action) will surface the expired-session redirect if it truly failed.
          })
      },
      Math.max(msUntilRefresh, 0),
    )
    return () => window.clearTimeout(timer)
  }, [token])

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
      login: (accessToken: string, refreshToken: string) => {
        setAccessToken(accessToken)
        setRefreshToken(refreshToken)
        setToken(accessToken)
      },
      logout: async () => {
        await logoutRequest()
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
