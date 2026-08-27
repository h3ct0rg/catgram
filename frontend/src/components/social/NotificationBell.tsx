import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useSession } from '../../context/SessionContext'
import { getUnreadNotificationCount } from '../../services/apiClient'

const POLL_INTERVAL_MS = 45000

export function NotificationBell() {
  const session = useSession()
  const navigate = useNavigate()
  const [unreadCount, setUnreadCount] = useState(0)

  useEffect(() => {
    if (!session.isAuthenticated) return undefined

    let cancelled = false
    async function poll() {
      if (document.visibilityState !== 'visible') return
      try {
        const count = await getUnreadNotificationCount()
        if (!cancelled) setUnreadCount(count)
      } catch {
        // ignore transient errors, retry on next interval
      }
    }

    poll()
    const interval = window.setInterval(poll, POLL_INTERVAL_MS)
    document.addEventListener('visibilitychange', poll)
    return () => {
      cancelled = true
      window.clearInterval(interval)
      document.removeEventListener('visibilitychange', poll)
    }
  }, [session.isAuthenticated])

  if (!session.isAuthenticated) return null

  return (
    <button
      className="notification-bell"
      aria-label="Notificaciones"
      onClick={() => navigate('/notifications')}
    >
      🔔
      {unreadCount > 0 && (
        <span className="notification-badge">{unreadCount > 9 ? '9+' : unreadCount}</span>
      )}
    </button>
  )
}
