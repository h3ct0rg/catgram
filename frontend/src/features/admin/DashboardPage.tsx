import { useEffect, useState } from 'react'
import { useSession } from '../../context/SessionContext'
import { getDashboardSummary, getShelterDashboardSummary } from '../../services/apiClient'
import { DashboardSummary, ShelterDashboardSummary } from '../../types/admin'

const GLOBAL_TILE_ICONS: Record<string, string> = {
  Usuarios: '👥',
  Refugios: '🏠',
  Mascotas: '🐾',
  Adoptados: '🏡',
  Publicaciones: '📸',
  'Historias activas': '⏳',
  Likes: '❤️',
  Comentarios: '💬',
  Compartidos: '🔁',
  Vistas: '👁️',
}

function GlobalDashboard() {
  const [summary, setSummary] = useState<DashboardSummary | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    getDashboardSummary()
      .then(setSummary)
      .catch(() => setError('No se pudo cargar el dashboard.'))
  }, [])

  if (error)
    return (
      <p className="feedback" role="status">
        {error}
      </p>
    )
  if (!summary) return <p className="body-copy">Cargando…</p>

  const tiles: Array<[string, number]> = [
    ['Usuarios', summary.users],
    ['Refugios', summary.shelters],
    ['Mascotas', summary.animals],
    ['Adoptados', summary.adoptedAnimals],
    ['Publicaciones', summary.posts],
    ['Historias activas', summary.activeStories],
    ['Likes', summary.likes],
    ['Comentarios', summary.comments],
    ['Compartidos', summary.shares],
    ['Vistas', summary.views],
  ]

  return (
    <div>
      <div className="admin-header">
        <div className="admin-header-title">
          <span className="admin-header-icon">📊</span>
          <div>
            <p className="eyebrow">Panel admin</p>
            <h1>Dashboard general</h1>
          </div>
        </div>
      </div>

      <div className="admin-grid">
        {tiles.map(([label, value]) => (
          <div className="glass-card admin-tile" key={label}>
            <span className="admin-tile-icon">{GLOBAL_TILE_ICONS[label]}</span>
            <div className="admin-tile-copy">
              <strong>{value.toLocaleString('es-ES')}</strong>
              <span>{label}</span>
            </div>
          </div>
        ))}
      </div>

      <h2 className="section-title">Mascotas por refugio</h2>
      <div className="admin-table">
        {summary.sheltersBreakdown.map((shelter) => (
          <div className="admin-row" key={shelter.shelterId}>
            <div>
              <strong>{shelter.shelterName}</strong>
              <p>
                {shelter.animalCount} mascotas · {shelter.adoptedCount} adoptados
              </p>
            </div>
          </div>
        ))}
        {summary.sheltersBreakdown.length === 0 && (
          <p className="admin-empty">Sin refugios todavía.</p>
        )}
      </div>

      <h2 className="section-title">Mascotas con más alcance</h2>
      <div className="admin-table">
        {summary.topAnimals.map((animal) => (
          <div className="admin-row" key={animal.animalId}>
            <div>
              <strong>{animal.animalName}</strong> · {animal.shelterName}
              <p>
                {animal.likes} likes · {animal.shares} compartidos · {animal.views} vistas
              </p>
            </div>
          </div>
        ))}
        {summary.topAnimals.length === 0 && (
          <p className="admin-empty">Todavía no hay actividad.</p>
        )}
      </div>
    </div>
  )
}

const SHELTER_TILE_ICONS: Record<string, string> = {
  Mascotas: '🐾',
  Adoptados: '🏡',
  Publicaciones: '📸',
  Likes: '❤️',
  Comentarios: '💬',
  Compartidos: '🔁',
  Vistas: '👁️',
  'Solicitudes pendientes': '📋',
}

function ShelterDashboard() {
  const [summary, setSummary] = useState<ShelterDashboardSummary | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    getShelterDashboardSummary()
      .then(setSummary)
      .catch(() => setError('No se pudo cargar el dashboard de tu refugio.'))
  }, [])

  if (error)
    return (
      <p className="feedback" role="status">
        {error}
      </p>
    )
  if (!summary) return <p className="body-copy">Cargando…</p>

  const tiles: Array<[string, number]> = [
    ['Mascotas', summary.animals],
    ['Adoptados', summary.adoptedAnimals],
    ['Publicaciones', summary.posts],
    ['Likes', summary.likes],
    ['Comentarios', summary.comments],
    ['Compartidos', summary.shares],
    ['Vistas', summary.views],
    ['Solicitudes pendientes', summary.pendingAdoptionRequests],
  ]

  return (
    <div>
      <div className="admin-header">
        <div className="admin-header-title">
          <span className="admin-header-icon">🏠</span>
          <div>
            <p className="eyebrow">Panel admin</p>
            <h1>Dashboard de tu refugio</h1>
          </div>
        </div>
      </div>

      <div className="admin-grid">
        {tiles.map(([label, value]) => (
          <div className="glass-card admin-tile" key={label}>
            <span className="admin-tile-icon">{SHELTER_TILE_ICONS[label]}</span>
            <div className="admin-tile-copy">
              <strong>{value.toLocaleString('es-ES')}</strong>
              <span>{label}</span>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}

export function DashboardPage() {
  const session = useSession()
  return session.roles.includes('SuperAdministrador') ? <GlobalDashboard /> : <ShelterDashboard />
}
