import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { BottomNav } from '../../components/layout/BottomNav'
import { useSession } from '../../context/SessionContext'
import { getAnimal, getAnimalStats, getShelter } from '../../services/apiClient'
import { Animal, Shelter } from '../../types/domain'
import { AnimalStats } from '../../types/admin'
import {
  adoptionStatusColor,
  adoptionStatusIcon,
  adoptionStatusLabel,
} from '../../utils/adoptionStatus'
import { SEX_OPTIONS, SIZE_OPTIONS } from '../../utils/animalOptions'
import { ShareSheet } from '../feed/ShareSheet'
import { AdoptionRequestModal } from '../adoption/AdoptionRequestModal'
import { FollowButton } from './FollowButton'
import { ShelterLocationMap } from '../../components/map/ShelterLocationMap'

const ADMIN_ROLES = ['Administrador', 'SuperAdministrador']

export function PetView() {
  const { animalId } = useParams<{ animalId: string }>()
  const navigate = useNavigate()
  const session = useSession()
  const [animal, setAnimal] = useState<Animal | null>(null)
  const [shelter, setShelter] = useState<Shelter | null>(null)
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
        if (!cancelled) {
          setAnimal(result)
          if (result.shelterId) {
            getShelter(result.shelterId)
              .then((s) => {
                if (!cancelled) setShelter(s)
              })
              .catch(() => undefined)
          }
        }
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
      <div className="app-shell">
        <main className="pet-shell">
          <button className="back-button" onClick={() => navigate(-1)}>
            ‹ Volver
          </button>
          <p className="body-copy">Cargando…</p>
        </main>
        <BottomNav
          onHome={() => navigate('/')}
          onSearch={() => navigate('/search')}
          onCreate={() => navigate('/animals/new')}
        />
      </div>
    )
  }

  if (error || !animal) {
    return (
      <div className="app-shell">
        <main className="pet-shell">
          <button className="back-button" onClick={() => navigate(-1)}>
            ‹ Volver
          </button>
          <p className="body-copy">{error || 'Mascota no encontrada.'}</p>
          <button className="primary-button" onClick={() => navigate('/')}>
            Volver al muro
          </button>
        </main>
        <BottomNav
          onHome={() => navigate('/')}
          onSearch={() => navigate('/search')}
          onCreate={() => navigate('/animals/new')}
        />
      </div>
    )
  }

  const primaryMedia = animal.media.find((media) => media.isPrimary) ?? animal.media[0]
  const gallery = animal.media.filter((media) => media !== primaryMedia)
  const sizeLabel =
    SIZE_OPTIONS.find((option) => option.value === animal.size)?.label ?? animal.size
  const sexLabel = SEX_OPTIONS.find((option) => option.value === animal.sex)?.label ?? animal.sex

  return (
    <div className="app-shell">
      <main className="pet-shell">
        <section className="pet-hero">
          {primaryMedia && <img src={primaryMedia.url} alt={animal.name} />}
          <div className="pet-hero-scrim" />
          <button className="pet-hero-back" onClick={() => navigate(-1)} aria-label="Volver">
            <span className="material-symbols-outlined">arrow_back</span>
          </button>
          <button
            className="pet-hero-share"
            onClick={() => setShowShare(true)}
            aria-label="Compartir"
          >
            <span className="material-symbols-outlined">share</span>
          </button>
          <div className="pet-hero-info">
            <span
              className="status"
              style={{ background: adoptionStatusColor(animal.adoptionStatus) }}
            >
              {adoptionStatusIcon(animal.adoptionStatus)}{' '}
              {adoptionStatusLabel(animal.adoptionStatus)}
            </span>
            <h1>{animal.name}</h1>
          </div>
        </section>
        <section className="pet-grid">
          <div>
            <span className="pet-stat-icon material-symbols-outlined">pets</span>
            <small>Raza</small>
            <strong>{animal.breed ?? 'Sin dato'}</strong>
          </div>
          <div>
            <span className="pet-stat-icon material-symbols-outlined">cake</span>
            <small>Edad</small>
            <strong>
              {animal.ageMonths ? `${Math.round(animal.ageMonths / 12)} años` : 'Sin dato'}
            </strong>
          </div>
          <div>
            <span className="pet-stat-icon material-symbols-outlined">straighten</span>
            <small>Tamaño</small>
            <strong>{sizeLabel}</strong>
          </div>
          <div>
            <span className="pet-stat-icon material-symbols-outlined">male</span>
            <small>Sexo</small>
            <strong>{sexLabel}</strong>
          </div>
        </section>
        <div className="pet-cta-row">
          <FollowButton animalId={animal.id} />
          <button
            className="primary-button adopt-button"
            onClick={() =>
              session.isAuthenticated ? setShowAdoptionRequest(true) : navigate('/login')
            }
          >
            <span
              className="material-symbols-outlined"
              style={{ fontVariationSettings: "'FILL' 1" }}
            >
              favorite
            </span>
            Adoptar a {animal.name}
          </button>
        </div>
        <section className="glass-card about">
          <h2>
            <span className="material-symbols-outlined">info</span> Acerca de {animal.name}
          </h2>
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
          <section className="pet-gallery-section">
            <h2 className="section-title">Galería</h2>
            <div className="pet-gallery-scroll">
              {gallery.map((media) => (
                <img src={media.url} alt={animal.name} key={media.id} />
              ))}
            </div>
          </section>
        )}
        <div className="location glass-card">
          <h2>
            <span className="material-symbols-outlined">location_on</span> Ubicación
          </h2>
          <ShelterLocationMap
            latitude={shelter?.latitude ?? null}
            longitude={shelter?.longitude ?? null}
            shelterName={animal.shelterName}
            address={shelter?.address}
            city={shelter?.city}
          />
        </div>

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
      <BottomNav
        onHome={() => navigate('/')}
        onSearch={() => navigate('/search')}
        onCreate={() => navigate('/animals/new')}
      />
    </div>
  )
}
