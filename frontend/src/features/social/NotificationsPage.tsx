import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  getNotificationPreferences,
  getNotifications,
  markAllNotificationsRead,
  markNotificationRead,
  updateNotificationPreference,
} from '../../services/apiClient'
import { Notification, NotificationPreference, NotificationType } from '../../types/social'
import { formatRelativeTime } from '../../utils/formatRelativeTime'

const TYPE_LABEL: Record<NotificationType, string> = {
  Like: 'Me gusta',
  Comment: 'Comentarios',
  Reply: 'Respuestas',
  AdoptionStatusChanged: 'Cambios de adopción',
  NewPost: 'Nuevas publicaciones de animales que sigues',
}

export function NotificationsPage() {
  const navigate = useNavigate()
  const [notifications, setNotifications] = useState<Notification[]>([])
  const [preferences, setPreferences] = useState<NotificationPreference[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [showPreferences, setShowPreferences] = useState(false)

  useEffect(() => {
    let cancelled = false
    Promise.all([getNotifications({ pageSize: 50 }), getNotificationPreferences()])
      .then(([page, prefs]) => {
        if (cancelled) return
        setNotifications(page.items)
        setPreferences(prefs)
      })
      .catch(() => {
        if (!cancelled) setError('No se pudieron cargar las notificaciones.')
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  async function markRead(notification: Notification) {
    if (notification.isRead) return
    setNotifications((current) =>
      current.map((item) => (item.id === notification.id ? { ...item, isRead: true } : item)),
    )
    try {
      await markNotificationRead(notification.id)
    } catch {
      /* keep optimistic state */
    }
  }

  async function markAllRead() {
    setNotifications((current) => current.map((item) => ({ ...item, isRead: true })))
    try {
      await markAllNotificationsRead()
    } catch {
      /* keep optimistic state */
    }
  }

  async function togglePreference(type: NotificationType, enabled: boolean) {
    setPreferences((current) =>
      current.map((pref) => (pref.type === type ? { ...pref, enabled } : pref)),
    )
    try {
      await updateNotificationPreference(type, enabled)
    } catch {
      /* keep optimistic state */
    }
  }

  return (
    <main className="feed-page notifications-page">
      <button className="back-button" onClick={() => navigate('/')}>
        ‹ Volver
      </button>
      <div className="feed-heading">
        <div>
          <p className="eyebrow">Centro de notificaciones</p>
          <h1>Novedades</h1>
        </div>
        <button className="secondary-button" onClick={() => setShowPreferences((value) => !value)}>
          Preferencias
        </button>
      </div>

      {showPreferences && (
        <section className="glass-card notification-preferences">
          {preferences.map((pref) => (
            <label className="preference-row" key={pref.type}>
              <span>{TYPE_LABEL[pref.type]}</span>
              <input
                type="checkbox"
                checked={pref.enabled}
                onChange={(event) => togglePreference(pref.type, event.target.checked)}
              />
            </label>
          ))}
        </section>
      )}

      {notifications.some((item) => !item.isRead) && (
        <button className="secondary-button" onClick={markAllRead}>
          Marcar todas como leídas
        </button>
      )}

      {loading && <p className="body-copy">Cargando…</p>}
      {!loading && error && (
        <p className="feedback" role="status">
          {error}
        </p>
      )}
      {!loading && !error && notifications.length === 0 && (
        <p className="body-copy">No tienes notificaciones todavía.</p>
      )}

      <div className="notification-list">
        {notifications.map((notification) => (
          <button
            className={`notification-item ${notification.isRead ? '' : 'unread'}`}
            key={notification.id}
            onClick={() => {
              markRead(notification)
              if (notification.linkUrl) navigate(notification.linkUrl)
            }}
          >
            <strong>{notification.title}</strong>
            <p>{notification.body}</p>
            <small>{formatRelativeTime(notification.createdAt)}</small>
          </button>
        ))}
      </div>
    </main>
  )
}
