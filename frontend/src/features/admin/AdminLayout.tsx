import { NavLink, Outlet, useNavigate } from 'react-router-dom'

export function AdminLayout() {
  const navigate = useNavigate()
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
          <NavLink to="/admin/users">Usuarios</NavLink>
          <NavLink to="/admin/audit">Auditoría</NavLink>
        </nav>
        <Outlet />
      </main>
    </div>
  )
}
