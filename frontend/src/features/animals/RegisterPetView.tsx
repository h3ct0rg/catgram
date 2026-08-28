import { ChangeEvent, FormEvent, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useSession } from '../../context/SessionContext'
import { addAnimalMedia, createAnimal, getAnimal, updateAnimal } from '../../services/apiClient'
import { adoptionStatusLabel } from '../../utils/adoptionStatus'
import { SEX_OPTIONS, SIZE_OPTIONS, SPECIES_OPTIONS } from '../../utils/animalOptions'

const ADOPTION_STATUS_OPTIONS = ['Available', 'InProcess', 'Adopted', 'Unavailable', 'Deceased']

export function RegisterPetView() {
  const navigate = useNavigate()
  const session = useSession()
  const { animalId } = useParams<{ animalId?: string }>()
  const isEditing = Boolean(animalId)

  const [loading, setLoading] = useState(isEditing)
  const [name, setName] = useState('')
  const [species, setSpecies] = useState(SPECIES_OPTIONS[0].value)
  const [sex, setSex] = useState(SEX_OPTIONS[2].value)
  const [size, setSize] = useState(SIZE_OPTIONS[1].value)
  const [ageMonths, setAgeMonths] = useState('')
  const [breed, setBreed] = useState('')
  const [location, setLocation] = useState('')
  const [description, setDescription] = useState('')
  const [adoptionStatus, setAdoptionStatus] = useState('Available')
  const [photo, setPhoto] = useState<File | null>(null)
  const [photoPreview, setPhotoPreview] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!animalId) return
    getAnimal(animalId)
      .then((animal) => {
        setName(animal.name)
        setSpecies(animal.species)
        setSex(animal.sex)
        setSize(animal.size)
        setAgeMonths(animal.ageMonths ? String(animal.ageMonths) : '')
        setBreed(animal.breed ?? '')
        setLocation(animal.location ?? '')
        setDescription(animal.description)
        setAdoptionStatus(animal.adoptionStatus)
        const primary = animal.media.find((media) => media.isPrimary) ?? animal.media[0]
        if (primary) setPhotoPreview(primary.thumbnailUrl ?? primary.url)
      })
      .catch(() => setError('No se pudo cargar la mascota.'))
      .finally(() => setLoading(false))
  }, [animalId])

  function handlePhotoChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0] ?? null
    setPhoto(file)
    setPhotoPreview(file ? URL.createObjectURL(file) : '')
  }

  async function submit(event: FormEvent) {
    event.preventDefault()
    setSubmitting(true)
    setError('')
    try {
      if (isEditing && animalId) {
        await updateAnimal(animalId, {
          name,
          species,
          sex,
          size,
          ageMonths: ageMonths ? Number(ageMonths) : undefined,
          breed: breed || undefined,
          description,
          location: location || undefined,
          adoptionStatus,
        })
        if (photo) await addAnimalMedia(animalId, photo, true)
        navigate('/admin/pets')
      } else {
        if (!session.shelterId) {
          setError('No tienes un refugio asignado.')
          return
        }
        const animal = await createAnimal({
          shelterId: session.shelterId,
          name,
          species,
          sex,
          size,
          ageMonths: ageMonths ? Number(ageMonths) : undefined,
          breed: breed || undefined,
          description,
          location: location || undefined,
        })
        if (photo) await addAnimalMedia(animal.id, photo, true)
        navigate(`/animals/${animal.id}`)
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo guardar la mascota.')
    } finally {
      setSubmitting(false)
    }
  }

  if (loading) return <p className="body-copy">Cargando…</p>

  return (
    <main className="register-shell">
      <button className="back-button" onClick={() => navigate(isEditing ? '/admin/pets' : '/')}>
        ‹ Volver
      </button>
      <section className="register-heading">
        <p className="eyebrow">Panel de refugio</p>
        <h1>{isEditing ? 'Editar mascota' : 'Registrar mascota'}</h1>
        <p className="body-copy">Comparte su historia y ayúdala a encontrar un hogar.</p>
      </section>
      <form className="register-form" onSubmit={submit}>
        <div className="upload-field">
          <span className="field-label">Foto principal</span>
          <label htmlFor="pet-photo" className="upload-box">
            {photoPreview ? (
              <img src={photoPreview} alt="Vista previa" className="upload-preview" />
            ) : (
              <>
                ＋<strong>Agregar foto</strong>
                <small>JPG, PNG o WEBP</small>
              </>
            )}
          </label>
          <input id="pet-photo" type="file" accept="image/*" hidden onChange={handlePhotoChange} />
        </div>
        <label>
          Nombre
          <input
            value={name}
            onChange={(event) => setName(event.target.value)}
            placeholder="Ej. Luna"
            required
          />
        </label>
        <div className="two-columns">
          <label>
            Especie
            <select value={species} onChange={(event) => setSpecies(event.target.value)}>
              {SPECIES_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <label>
            Sexo
            <select value={sex} onChange={(event) => setSex(event.target.value)}>
              {SEX_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
        </div>
        <div className="two-columns">
          <label>
            Edad (meses)
            <input
              type="number"
              min="0"
              value={ageMonths}
              onChange={(event) => setAgeMonths(event.target.value)}
              placeholder="Ej. 24"
            />
          </label>
          <label>
            Tamaño
            <select value={size} onChange={(event) => setSize(event.target.value)}>
              {SIZE_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
        </div>
        <label>
          Raza (opcional)
          <input
            value={breed}
            onChange={(event) => setBreed(event.target.value)}
            placeholder="Ej. Labrador"
          />
        </label>
        <label>
          Ubicación (opcional)
          <input value={location} onChange={(event) => setLocation(event.target.value)} />
        </label>
        <label>
          Descripción y personalidad
          <textarea
            rows={5}
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            placeholder="Cuéntanos sobre sus gustos y personalidad…"
            required
          />
        </label>
        {isEditing && (
          <label>
            Estado de adopción
            <select
              value={adoptionStatus}
              onChange={(event) => setAdoptionStatus(event.target.value)}
            >
              {ADOPTION_STATUS_OPTIONS.map((status) => (
                <option key={status} value={status}>
                  {adoptionStatusLabel(status)}
                </option>
              ))}
            </select>
          </label>
        )}
        {error && (
          <p className="feedback" role="status">
            {error}
          </p>
        )}
        <button className="primary-button" type="submit" disabled={submitting}>
          {submitting ? 'Guardando…' : isEditing ? '💾 Guardar cambios' : '🐾 Registrar mascota'}
        </button>
      </form>
    </main>
  )
}
