import { useEffect, useState } from 'react'
import { getDashboardSummary } from '../../services/apiClient'
import { DashboardSummary } from '../../types/admin'

export function DashboardPage() {
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
    ['Animales', summary.animals],
    ['Adoptados', summary.adoptedAnimals],
    ['Publicaciones', summary.posts],
    ['Historias activas', summary.activeStories],
    ['Likes', summary.likes],
    ['Comentarios', summary.comments],
    ['Compartidos', summary.shares],
    ['Vistas', summary.views],
  ]

  return (
    <div className="admin-grid">
      {tiles.map(([label, value]) => (
        <div className="glass-card admin-tile" key={label}>
          <strong>{value.toLocaleString('es-ES')}</strong>
          <span>{label}</span>
        </div>
      ))}
    </div>
  )
}
