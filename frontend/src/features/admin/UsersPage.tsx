import { useEffect, useState } from 'react'
import { assignUserRole, getUsers, setUserStatus } from '../../services/apiClient'
import { AdminUser } from '../../types/admin'

const ROLES = ['Usuario', 'Administrador', 'SuperAdministrador']

export function UsersPage() {
  const [users, setUsers] = useState<AdminUser[]>([])
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    getUsers()
      .then(setUsers)
      .catch(() => setError('No se pudieron cargar los usuarios.'))
      .finally(() => setLoading(false))
  }, [])

  async function toggleActive(user: AdminUser) {
    try {
      await setUserStatus(user.id, !user.isActive)
      setUsers((current) =>
        current.map((u) => (u.id === user.id ? { ...u, isActive: !u.isActive } : u)),
      )
    } catch {
      setError('No se pudo actualizar el usuario.')
    }
  }

  async function changeRole(user: AdminUser, role: string) {
    try {
      await assignUserRole(user.id, role)
      setUsers((current) => current.map((u) => (u.id === user.id ? { ...u, roles: [role] } : u)))
    } catch {
      setError('No se pudo cambiar el rol.')
    }
  }

  return (
    <div>
      <div className="admin-header">
        <div className="admin-header-title">
          <span className="admin-header-icon">👥</span>
          <div>
            <p className="eyebrow">Panel admin</p>
            <h1>Usuarios</h1>
          </div>
        </div>
      </div>

      {loading && <p className="body-copy">Cargando…</p>}
      {error && (
        <p className="feedback" role="status">
          {error}
        </p>
      )}
      <div className="admin-table">
        {users.map((user) => (
          <div className="admin-row" key={user.id}>
            <div>
              <strong>{user.userName}</strong> · {user.email}
              <p>{user.fullName}</p>
            </div>
            <div className="admin-row-actions">
              <select
                value={user.roles[0] ?? ''}
                onChange={(event) => changeRole(user, event.target.value)}
              >
                {ROLES.map((role) => (
                  <option key={role} value={role}>
                    {role}
                  </option>
                ))}
              </select>
              <button
                className={user.isActive ? 'secondary-button' : 'primary-button'}
                onClick={() => toggleActive(user)}
              >
                {user.isActive ? 'Bloquear' : 'Desbloquear'}
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
