import { ReactElement } from 'react'
import { Navigate } from 'react-router-dom'
import { useSession } from '../context/SessionContext'

type Props = { roles: string[]; children: ReactElement }

export function RequireRole({ roles, children }: Props) {
  const session = useSession()
  if (!session.isAuthenticated) return <Navigate to="/login" replace />
  if (!roles.some((role) => session.roles.includes(role)))
    return <Navigate to="/forbidden" replace />
  return children
}
