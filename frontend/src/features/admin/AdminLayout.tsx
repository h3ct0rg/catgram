import { useEffect, useState } from 'react'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useSession } from '../../context/SessionContext'

type NavItem = {
  to: string
  end?: boolean
  icon: string
  label: string
  visibility: 'all' | 'super' | 'shelter'
}

const NAV_ITEMS: NavItem[] = [
  { to: '/admin', end: true, icon: '📊', label: 'Dashboard', visibility: 'all' },
  { to: '/admin/reports', icon: '🚩', label: 'Reportes', visibility: 'all' },
  { to: '/admin/adoptions', icon: '🐕', label: 'Solicitudes', visibility: 'all' },
  { to: '/admin/pets', icon: '🐾', label: 'Mascotas', visibility: 'shelter' },
  { to: '/admin/posts/new', icon: '📸', label: 'Publicar', visibility: 'all' },
  { to: '/admin/users', icon: '👥', label: 'Usuarios', visibility: 'super' },
  { to: '/admin/audit', icon: '🧾', label: 'Auditoría', visibility: 'super' },
  { to: '/admin/invite', icon: '✉️', label: 'Invitar', visibility: 'super' },
  { to: '/admin/shelter', icon: '🏠', label: 'Mi refugio', visibility: 'shelter' },
]

export function AdminLayout() {
  const navigate = useNavigate()
  const location = useLocation()
  const session = useSession()
  const isSuperAdmin = session.roles.includes('SuperAdministrador')
  const [menuOpen, setMenuOpen] = useState(false)

  useEffect(() => {
    setMenuOpen(false)
  }, [location.pathname])

  const items = NAV_ITEMS.filter((item) => {
    if (item.visibility === 'super') return isSuperAdmin
    if (item.visibility === 'shelter') return !isSuperAdmin
    return true
  })

  async function handleLogout() {
    await session.logout()
    navigate('/')
  }

  return (
    <div className="app-shell">
      <header className="topbar">
        <button
          className="hamburger"
          aria-label="Abrir menú de administración"
          onClick={() => setMenuOpen(true)}
        >
          ☰
        </button>
        <button className="brand" onClick={() => navigate('/')}>
          <span className="paw">🐾</span>
          <span>Panel admin</span>
        </button>
        <div className="topbar-actions">
          <span className="badge badge-role">{isSuperAdmin ? 'SuperAdmin' : 'Administrador'}</span>
          <button className="avatar" aria-label="Volver al muro" onClick={() => navigate('/')}>
            👩🏻
          </button>
        </div>
      </header>

      <div className="admin-layout">
        {menuOpen && (
          <button
            className="admin-sidebar-overlay"
            aria-label="Cerrar menú"
            onClick={() => setMenuOpen(false)}
          />
        )}
        <aside className={`admin-sidebar ${menuOpen ? 'open' : ''}`}>
          <nav className="admin-nav">
            {items.map((item) => (
              <NavLink key={item.to} to={item.to} end={item.end}>
                <span>{item.icon}</span> {item.label}
              </NavLink>
            ))}
            <button className="admin-nav-logout" onClick={handleLogout}>
              <span>🚪</span> Cerrar sesión
            </button>
          </nav>
        </aside>
        <main className="admin-content admin-page">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
