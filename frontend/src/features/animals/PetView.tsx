import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useSession } from '../../context/SessionContext'
import { getAnimal, getAnimalStats } from '../../services/apiClient'
import { Animal } from '../../types/domain'
import { AnimalStats } from '../../types/admin'
import {
  adoptionStatusColor,
  adoptionStatusIcon,
  adoptionStatusLabel,
} from '../../utils/adoptionStatus'
import { ShareSheet } from '../feed/ShareSheet'
import { AdoptionRequestModal } from '../adoption/AdoptionRequestModal'
import { FollowButton } from './FollowButton'

const ADMIN_ROLES = ['Administrador', 'SuperAdministrador']

export function PetView() {
  const { animalId } = useParams<{ animalId: string }>()
  const navigate = useNavigate()
  const session = useSession()
  const [animal, setAnimal] = useState<Animal | null>(null)
  const [stats, setStats] = useState<AnimalStats | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [showShare, setShowShare] = useState(false)
  const [showAdoptionRequest, setShowAdoptionRequest] = useState(false)
  const isAdmin = session.roles.some((role) => ADMIN_ROLES.includes(role))

  useEffect(() => {
    if (!animalId) return
    let cancelled = false
    setLoading(true)
    getAnimal(animalId)
      .then((result) => {
        if (!cancelled) setAnimal(result)
      })
      .catch(() => {
        if (!cancelled) setError('No pudimos encontrar a esta mascota.')
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [animalId])

  useEffect(() => {
    if (!animalId || !isAdmin) return
    getAnimalStats(animalId)
      .then(setStats)
      .catch(() => undefined)
  }, [animalId, isAdmin])

  if (loading) {
    return (
      <main className="pet-shell">
        <button className="back-button" onClick={() => navigate('/')}>
          ‹ Volver
        </button>
        <p className="body-copy">Cargando…</p>
      </main>
    )
  }

  if (error || !animal) {
    return (
      <main className="pet-shell">
        <button className="back-button" onClick={() => navigate('/')}>
          ‹ Volver
        </button>
        <p className="body-copy">{error || 'Mascota no encontrada.'}</p>
        <button className="primary-button" onClick={() => navigate('/')}>
          Volver al muro
        </button>
      </main>
    )
  }

  const primaryMedia = animal.media.find((media) => media.isPrimary) ?? animal.media[0]
  const gallery = animal.media.filter((media) => media !== primaryMedia)

  return (
    <main className="pet-shell">
      <button className="back-button" onClick={() => navigate('/')}>
        ‹ Volver
      </button>
      <section className="pet-hero">
        {primaryMedia && <img src={primaryMedia.url} alt={animal.name} />}
        <span className="status" style={{ background: adoptionStatusColor(animal.adoptionStatus) }}>
          {adoptionStatusIcon(animal.adoptionStatus)} {adoptionStatusLabel(animal.adoptionStatus)}
        </span>
        <h1>{animal.name}</h1>
      </section>
      <section className="pet-grid">
        <div>
          <small>RAZA</small>
          <strong>{animal.breed ?? 'Sin dato'}</strong>
        </div>
        <div>
          <small>EDAD</small>
          <strong>
            {animal.ageMonths ? `${Math.round(animal.ageMonths / 12)} años` : 'Sin dato'}
          </strong>
        </div>
        <div>
          <small>TAMAÑO</small>
          <strong>{animal.size}</strong>
        </div>
        <div>
          <small>SEXO</small>
          <strong>{animal.sex}</strong>
        </div>
      </section>
      <div className="pet-social-actions">
        <FollowButton animalId={animal.id} />
        <button className="secondary-button" onClick={() => setShowShare(true)}>
          ⌁ Compartir
        </button>
      </div>
      <section className="glass-card about">
        <h2>ⓘ &nbsp;Acerca de {animal.name}</h2>
        <p>{animal.description}</p>
      </section>
      {isAdmin && stats && (
        <section className="glass-card animal-stats">
          <h2>📊 &nbsp;Alcance (solo staff)</h2>
          <div className="admin-grid">
            <div className="admin-tile">
              <strong>{stats.postCount}</strong>
              <span>Publicaciones</span>
            </div>
            <div className="admin-tile">
              <strong>{stats.totalLikes}</strong>
              <span>Likes</span>
            </div>
            <div className="admin-tile">
              <strong>{stats.totalComments}</strong>
              <span>Comentarios</span>
            </div>
            <div className="admin-tile">
              <strong>{stats.totalViews}</strong>
              <span>Vistas</span>
            </div>
            <div className="admin-tile">
              <strong>{stats.totalShares}</strong>
              <span>Compartidos</span>
            </div>
            <div className="admin-tile">
              <strong>{stats.followerCount}</strong>
              <span>Seguidores</span>
            </div>
          </div>
        </section>
      )}
      {gallery.length > 0 && (
        <>
          <h2 className="section-title">Galería</h2>
          <div className="gallery">
            {gallery.map((media) => (
              <img src={media.url} alt={animal.name} key={media.id} />
            ))}
          </div>
        </>
      )}
      <div className="location glass-card">
        <h2>⌖ &nbsp;Ubicación</h2>
        <p>
          {animal.shelterName}
          {animal.location ? ` · ${animal.location}` : ''}
        </p>
      </div>
      <button
        className="primary-button adopt-button"
        onClick={() =>
          session.isAuthenticated ? setShowAdoptionRequest(true) : navigate('/login')
        }
      >
        ♡ &nbsp;Adoptar a {animal.name}
      </button>

      {showShare && (
        <ShareSheet
          url={`${window.location.origin}/animals/${animal.id}`}
          title={animal.name}
          text={`Ayuda a ${animal.name} a encontrar un hogar`}
          onClose={() => setShowShare(false)}
        />
      )}
      {showAdoptionRequest && (
        <AdoptionRequestModal
          animalId={animal.id}
          animalName={animal.name}
          onClose={() => setShowAdoptionRequest(false)}
        />
      )}
    </main>
  )
}
