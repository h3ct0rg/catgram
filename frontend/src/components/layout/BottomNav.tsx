import { useNavigate } from 'react-router-dom'
import { useSession } from '../../context/SessionContext'

type Props = {
  onHome: () => void
  onSearch: () => void
  onCreate: () => void
}

const ADMIN_ROLES = ['Administrador', 'SuperAdministrador']

export function BottomNav({ onHome, onSearch, onCreate }: Props) {
  const navigate = useNavigate()
  const session = useSession()

  function goToProfile() {
    if (!session.isAuthenticated) navigate('/login')
    else if (session.roles.some((role) => ADMIN_ROLES.includes(role))) navigate('/admin')
    else navigate('/notifications')
  }

  return (
    <nav className="bottom-nav">
      <button className="active" onClick={onHome}>
        ⌂<small>Inicio</small>
      </button>
      <button onClick={onSearch}>
        ⌕<small>Buscar</small>
      </button>
      <button className="create" onClick={onCreate}>
        ＋
      </button>
      <button onClick={goToProfile}>
        ♙<small>Perfil</small>
      </button>
    </nav>
  )
}
