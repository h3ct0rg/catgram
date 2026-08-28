import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useSession } from '../../context/SessionContext'

export function AdminLayout() {
  const navigate = useNavigate()
  const session = useSession()
  const isSuperAdmin = session.roles.includes('SuperAdministrador')

  return (
    <div className="app-shell">
      <header className="topbar">
        <button className="brand" onClick={() => navigate('/')}>
          <span className="paw">🐾</span>
          <span>Panel admin</span>
        </button>
      </header>
      <main className="feed-page admin-page">
        <nav className="admin-tabs">
          <NavLink to="/admin" end>
            Dashboard
          </NavLink>
          <NavLink to="/admin/reports">Reportes</NavLink>
          <NavLink to="/admin/adoptions">Solicitudes</NavLink>
          <NavLink to="/admin/posts/new">Publicar</NavLink>
          {isSuperAdmin && <NavLink to="/admin/users">Usuarios</NavLink>}
          {isSuperAdmin && <NavLink to="/admin/audit">Auditoría</NavLink>}
          {isSuperAdmin && <NavLink to="/admin/invite">Invitar</NavLink>}
          {!isSuperAdmin && <NavLink to="/admin/shelter">Mi refugio</NavLink>}
        </nav>
        <Outlet />
      </main>
    </div>
  )
}
