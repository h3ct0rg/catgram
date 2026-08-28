import { useLocation, useNavigate } from 'react-router-dom'
import { useSession } from '../../context/SessionContext'

type Props = {
  onHome: () => void
  onSearch: () => void
  onCreate: () => void
}

const ADMIN_ROLES = ['Administrador', 'SuperAdministrador']

export function BottomNav({ onHome, onSearch, onCreate }: Props) {
  const navigate = useNavigate()
  const location = useLocation()
  const session = useSession()
  const isAdmin = session.roles.some((role) => ADMIN_ROLES.includes(role))

  const isHome = location.pathname === '/'
  const isSearch = location.pathname === '/search'
  const isProfile = location.pathname === '/notifications' || location.pathname.startsWith('/admin')

  function goToProfile() {
    if (!session.isAuthenticated) navigate('/login')
    else if (isAdmin) navigate('/admin')
    else navigate('/notifications')
  }

  return (
    <nav className="bottom-nav">
      <button className={isHome ? 'active' : ''} onClick={onHome}>
        <span
          className="material-symbols-outlined"
          style={{ fontVariationSettings: `'FILL' ${isHome ? 1 : 0}` }}
        >
          home
        </span>
        <small>Inicio</small>
      </button>
      <button className={isSearch ? 'active' : ''} onClick={onSearch}>
        <span
          className="material-symbols-outlined"
          style={{ fontVariationSettings: `'FILL' ${isSearch ? 1 : 0}` }}
        >
          search
        </span>
        <small>Buscar</small>
      </button>
      {isAdmin && (
        <button className="create" onClick={onCreate} aria-label="Crear">
          <span className="create-icon material-symbols-outlined">add</span>
        </button>
      )}
      <button className={isProfile ? 'active' : ''} onClick={goToProfile}>
        <span
          className="material-symbols-outlined"
          style={{ fontVariationSettings: `'FILL' ${isProfile ? 1 : 0}` }}
        >
          person
        </span>
        <small>Perfil</small>
      </button>
    </nav>
  )
}
