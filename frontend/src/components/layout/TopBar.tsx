import { useNavigate } from 'react-router-dom'
import { useSession } from '../../context/SessionContext'
import { NotificationBell } from '../social/NotificationBell'

type Props = {
  onHome: () => void
}

const ADMIN_ROLES = ['Administrador', 'SuperAdministrador']

export function TopBar({ onHome }: Props) {
  const navigate = useNavigate()
  const session = useSession()

  function goToProfile() {
    if (!session.isAuthenticated) navigate('/login')
    else if (session.roles.some((role) => ADMIN_ROLES.includes(role))) navigate('/admin')
    else navigate('/notifications')
  }

  return (
    <header className="topbar">
      <button className="brand" onClick={onHome}>
        <span className="paw">🐾</span>
        <span>Kindred Paws</span>
      </button>
      <div className="topbar-actions">
        <NotificationBell />
        <button className="avatar" aria-label="Abrir perfil" onClick={goToProfile}>
          <span className="material-symbols-outlined">person</span>
        </button>
      </div>
    </header>
  )
}
