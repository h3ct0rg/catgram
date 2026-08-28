import { useEffect, useState, ChangeEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { BottomNav } from '../../components/layout/BottomNav'
import { useSession } from '../../context/SessionContext'
import {
  getNotificationPreferences,
  getNotifications,
  markAllNotificationsRead,
  markNotificationRead,
  updateNotificationPreference,
} from '../../services/apiClient'
import { Notification, NotificationPreference, NotificationType } from '../../types/social'
import { formatRelativeTime } from '../../utils/formatRelativeTime'
import { getInitials, stringToColor } from '../../utils/avatarColor'

type Tab = 'profile' | 'notifications' | 'preferences'

const TYPE_CONFIG: Record<
  NotificationType,
  { label: string; description: string; icon: string }
> = {
  Like: {
    label: 'Me gusta',
    description: 'Recibe alertas cuando a otros usuarios les gustan tus comentarios o publicaciones.',
    icon: 'favorite',
  },
  Comment: {
    label: 'Comentarios',
    description: 'Avisos cuando alguien comenta en publicaciones en las que has interactuado.',
    icon: 'chat_bubble',
  },
  Reply: {
    label: 'Respuestas a comentarios',
    description: 'Notificaciones inmediatas cuando alguien te responde directamente a un comentario.',
    icon: 'reply',
  },
  AdoptionStatusChanged: {
    label: 'Cambios de estado de adopción',
    description: 'Te avisa si una mascota que estás siguiendo fue Adoptada, puesta en Proceso o Disponible.',
    icon: 'pets',
  },
  NewPost: {
    label: 'Nuevas fotos e historias',
    description: 'Te enteras al instante cuando los refugios que sigues suben una nueva publicación de sus mascotas.',
    icon: 'photo_library',
  },
}

export function NotificationsPage() {
  const navigate = useNavigate()
  const session = useSession()
  const [activeTab, setActiveTab] = useState<Tab>('profile')
  const [notifications, setNotifications] = useState<Notification[]>([])
  const [preferences, setPreferences] = useState<NotificationPreference[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  // Avatar customization state (local persistence for custom user photo)
  const [customAvatar, setCustomAvatar] = useState<string>(() => {
    return localStorage.getItem(`custom_avatar_${session.userId}`) || ''
  })

  async function handleLogout() {
    await session.logout()
    navigate('/')
  }

  useEffect(() => {
    let cancelled = false
    Promise.all([getNotifications({ pageSize: 50 }), getNotificationPreferences()])
      .then(([page, prefs]) => {
        if (cancelled) return
        setNotifications(page.items)
        setPreferences(prefs)
      })
      .catch(() => {
        if (!cancelled) setError('No se pudieron cargar los datos del perfil.')
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  function handleAvatarUpload(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    if (file) {
      const reader = new FileReader()
      reader.onload = (e) => {
        const result = e.target?.result as string
        setCustomAvatar(result)
        if (session.userId) {
          localStorage.setItem(`custom_avatar_${session.userId}`, result)
        }
      }
      reader.readAsDataURL(file)
    }
  }

  function removeCustomAvatar() {
    setCustomAvatar('')
    if (session.userId) {
      localStorage.removeItem(`custom_avatar_${session.userId}`)
    }
  }

  async function markRead(notification: Notification) {
    if (notification.isRead) return
    setNotifications((current) =>
      current.map((item) => (item.id === notification.id ? { ...item, isRead: true } : item)),
    )
    try {
      await markNotificationRead(notification.id)
    } catch {
      /* optimistic */
    }
  }

  async function markAllRead() {
    setNotifications((current) => current.map((item) => ({ ...item, isRead: true })))
    try {
      await markAllNotificationsRead()
    } catch {
      /* optimistic */
    }
  }

  async function togglePreference(type: NotificationType, enabled: boolean) {
    setPreferences((current) =>
      current.map((pref) => (pref.type === type ? { ...pref, enabled } : pref)),
    )
    try {
      await updateNotificationPreference(type, enabled)
    } catch {
      /* optimistic */
    }
  }

  const unreadCount = notifications.filter((n) => !n.isRead).length
  const userInitials = getInitials(session.userName ?? 'U')
  const defaultBg = stringToColor(session.userId ?? session.userName ?? 'User')

  return (
    <div className="app-shell">
      <main className="feed-page user-profile-page">
        <button className="back-button" onClick={() => navigate(-1)}>
          ‹ Volver
        </button>

        {/* Profile Hero Header */}
        <section className="profile-hero-card glass-card">
          <div className="profile-hero-top">
            <div className="profile-avatar-wrapper">
              {customAvatar ? (
                <img src={customAvatar} alt="Foto de perfil" className="profile-avatar-img" />
              ) : (
                <div className="profile-avatar-initials" style={{ background: defaultBg }}>
                  {userInitials}
                </div>
              )}
              <label className="avatar-edit-badge" title="Cambiar foto de perfil">
                <span className="material-symbols-outlined">photo_camera</span>
                <input
                  type="file"
                  accept="image/*"
                  onChange={handleAvatarUpload}
                  style={{ display: 'none' }}
                />
              </label>
            </div>

            <div className="profile-hero-info">
              <h2>{session.userName ?? 'Usuario'}</h2>
              <span className="role-pill">
                <span className="material-symbols-outlined">verified_user</span>
                {session.roles.includes('SuperAdministrador')
                  ? 'Super Administrador'
                  : session.roles.includes('Administrador')
                  ? 'Administrador de Refugio'
                  : 'Amante de las Mascotas (Usuario)'}
              </span>
              <p className="profile-hero-subtitle">
                Explora historias, sigue adopciones y personaliza tus avisos.
              </p>
            </div>
          </div>

          {customAvatar && (
            <button type="button" className="remove-avatar-btn" onClick={removeCustomAvatar}>
              <span className="material-symbols-outlined">delete</span> Restaurar avatar por defecto
            </button>
          )}
        </section>

        {/* Segmented Navigation Tabs */}
        <div className="profile-tabs-nav" role="tablist">
          <button
            type="button"
            className={`profile-tab ${activeTab === 'profile' ? 'active' : ''}`}
            onClick={() => setActiveTab('profile')}
          >
            <span className="material-symbols-outlined">person</span>
            Mi Cuenta
          </button>
          <button
            type="button"
            className={`profile-tab ${activeTab === 'notifications' ? 'active' : ''}`}
            onClick={() => setActiveTab('notifications')}
          >
            <span className="material-symbols-outlined">notifications</span>
            Novedades
            {unreadCount > 0 && <span className="tab-badge">{unreadCount}</span>}
          </button>
          <button
            type="button"
            className={`profile-tab ${activeTab === 'preferences' ? 'active' : ''}`}
            onClick={() => setActiveTab('preferences')}
          >
            <span className="material-symbols-outlined">tune</span>
            Preferencias
          </button>
        </div>

        {/* TAB 1: Profile Details & Information */}
        {activeTab === 'profile' && (
          <section className="profile-tab-content">
            <div className="glass-card profile-details-card">
              <h3 className="section-subtitle">
                <span className="material-symbols-outlined">badge</span> Datos de la cuenta
              </h3>
              <div className="profile-details-grid">
                <div className="detail-item">
                  <small>Nombre completo / Usuario</small>
                  <strong>{session.userName ?? 'No definido'}</strong>
                </div>
                <div className="detail-item">
                  <small>Rol en la plataforma</small>
                  <strong>{session.roles.join(', ') || 'Usuario'}</strong>
                </div>
                <div className="detail-item">
                  <small>Estado de sesión</small>
                  <strong className="status-active">
                    <span className="dot" /> Activa
                  </strong>
                </div>
                {session.shelterId && (
                  <div className="detail-item">
                    <small>ID Refugio Asignado</small>
                    <strong className="mono">{session.shelterId}</strong>
                  </div>
                )}
              </div>
            </div>

            <div className="glass-card account-actions-card">
              <h3 className="section-subtitle">
                <span className="material-symbols-outlined">settings</span> Acciones
              </h3>
              <p className="body-copy">
                Puedes cerrar tu sesión de forma segura en este dispositivo en cualquier momento.
              </p>
              <button className="logout-button primary-button-danger" onClick={handleLogout}>
                <span className="material-symbols-outlined">logout</span>
                Cerrar sesión
              </button>
            </div>
          </section>
        )}

        {/* TAB 2: Notifications List */}
        {activeTab === 'notifications' && (
          <section className="profile-tab-content">
            <div className="tab-header-row">
              <h3>Historial de Notificaciones</h3>
              {notifications.some((item) => !item.isRead) && (
                <button className="secondary-button mark-read-all-btn" onClick={markAllRead}>
                  <span className="material-symbols-outlined">done_all</span>
                  Marcar leídas
                </button>
              )}
            </div>

            {loading && <p className="body-copy">Cargando novedades…</p>}
            {!loading && error && (
              <p className="feedback" role="status">
                {error}
              </p>
            )}
            {!loading && !error && notifications.length === 0 && (
              <div className="glass-card empty-state-box">
                <span className="material-symbols-outlined empty-icon">notifications_off</span>
                <p className="body-copy">No tienes notificaciones pendientes por ahora.</p>
              </div>
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
                  <div className="notification-item-header">
                    <strong>{notification.title}</strong>
                    {!notification.isRead && <span className="unread-dot" />}
                  </div>
                  <p>{notification.body}</p>
                  <small>{formatRelativeTime(notification.createdAt)}</small>
                </button>
              ))}
            </div>
          </section>
        )}

        {/* TAB 3: Preferences Config with Detailed Explanations */}
        {activeTab === 'preferences' && (
          <section className="profile-tab-content">
            <div className="glass-card preferences-intro-card">
              <h3 className="section-subtitle">
                <span className="material-symbols-outlined">tune</span> Centro de Configuración
              </h3>
              <p className="preferences-explainer">
                Personaliza qué eventos generan alertas en tu cuenta. Activa o desactiva los interruptores para recibir únicamente las notificaciones que te interesan.
              </p>

              <div className="preferences-items-list">
                {preferences.map((pref) => {
                  const conf = TYPE_CONFIG[pref.type] || {
                    label: pref.type,
                    description: 'Notificación personalizada',
                    icon: 'notifications',
                  }
                  return (
                    <div className="preference-item-card" key={pref.type}>
                      <div className="preference-item-icon">
                        <span className="material-symbols-outlined">{conf.icon}</span>
                      </div>
                      <div className="preference-item-text">
                        <strong>{conf.label}</strong>
                        <p>{conf.description}</p>
                      </div>
                      <label className="switch-toggle">
                        <input
                          type="checkbox"
                          checked={pref.enabled}
                          onChange={(event) => togglePreference(pref.type, event.target.checked)}
                        />
                        <span className="slider-round" />
                      </label>
                    </div>
                  )
                })}
              </div>
            </div>
          </section>
        )}
      </main>

      <BottomNav
        onHome={() => navigate('/')}
        onSearch={() => navigate('/search')}
        onCreate={() => navigate('/animals/new')}
      />
    </div>
  )
}

