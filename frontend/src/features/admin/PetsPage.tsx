import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useSession } from '../../context/SessionContext'
import { deleteAnimal, getAnimals } from '../../services/apiClient'
import { Animal } from '../../types/domain'

export function PetsPage() {
  const navigate = useNavigate()
  const session = useSession()
  const [pets, setPets] = useState<Animal[]>([])
  const [loading, setLoading] = useState(true)
  const [busyId, setBusyId] = useState('')
  const [error, setError] = useState('')

  useEffect(() => {
    if (!session.shelterId) {
      setLoading(false)
      return
    }
    getAnimals({ shelterId: session.shelterId })
      .then(setPets)
      .catch(() => setError('No se pudieron cargar las mascotas.'))
      .finally(() => setLoading(false))
  }, [session.shelterId])

  async function remove(pet: Animal) {
    if (!window.confirm(`¿Eliminar a ${pet.name}? Esto no se puede deshacer.`)) return
    setBusyId(pet.id)
    setError('')
    try {
      await deleteAnimal(pet.id)
      setPets((current) => current.filter((item) => item.id !== pet.id))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo eliminar la mascota.')
    } finally {
      setBusyId('')
    }
  }

  return (
    <div>
      <div className="admin-header">
        <div className="admin-header-title">
          <span className="admin-header-icon">🐾</span>
          <div>
            <p className="eyebrow">Panel admin</p>
            <h1>Mascotas</h1>
          </div>
        </div>
        <div className="admin-header-action">
          <button className="primary-button" type="button" onClick={() => navigate('/animals/new')}>
            Crear mascota
          </button>
        </div>
      </div>

      {loading && <p className="body-copy">Cargando…</p>}
      {error && (
        <p className="feedback" role="status">
          {error}
        </p>
      )}

      <div className="admin-table">
        {pets.map((pet) => {
          const thumb =
            pet.media.find((media) => media.isPrimary)?.thumbnailUrl ?? pet.media[0]?.thumbnailUrl
          return (
            <div className="admin-row" key={pet.id}>
              <div className="admin-row-pet">
                {thumb ? (
                  <img src={thumb} alt={pet.name} className="admin-row-thumb" />
                ) : (
                  <span className="admin-row-thumb-fallback">🐾</span>
                )}
                <strong>{pet.name}</strong>
              </div>
              <div className="admin-row-actions">
                <button
                  className="secondary-button"
                  onClick={() => navigate(`/animals/${pet.id}/edit`)}
                >
                  Editar
                </button>
                <button
                  className="danger-button"
                  disabled={busyId === pet.id}
                  onClick={() => remove(pet)}
                >
                  Eliminar
                </button>
              </div>
            </div>
          )
        })}
        {!loading && pets.length === 0 && (
          <p className="admin-empty">Todavía no tienes mascotas registradas.</p>
        )}
      </div>
    </div>
  )
}
