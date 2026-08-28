import { ChangeEvent, FormEvent, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useSession } from '../../context/SessionContext'
import { createPost, getAnimals } from '../../services/apiClient'
import { Animal } from '../../types/domain'

export function CreatePostPage() {
  const navigate = useNavigate()
  const session = useSession()
  const [animals, setAnimals] = useState<Animal[]>([])
  const [loadingAnimals, setLoadingAnimals] = useState(true)
  const [animalId, setAnimalId] = useState('')
  const [caption, setCaption] = useState('')
  const [location, setLocation] = useState('')
  const [hashtags, setHashtags] = useState('')
  const [isFeatured, setIsFeatured] = useState(false)
  const [isSuccessStory, setIsSuccessStory] = useState(false)
  const [files, setFiles] = useState<File[]>([])
  const [previews, setPreviews] = useState<string[]>([])
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')
  const [done, setDone] = useState(false)

  useEffect(() => {
    getAnimals(session.shelterId ? { shelterId: session.shelterId } : {})
      .then(setAnimals)
      .catch(() => undefined)
      .finally(() => setLoadingAnimals(false))
  }, [session.shelterId])

  function handleFilesChange(event: ChangeEvent<HTMLInputElement>) {
    previews.forEach((url) => URL.revokeObjectURL(url))
    const selected = Array.from(event.target.files ?? [])
    setFiles(selected)
    setPreviews(selected.map((file) => URL.createObjectURL(file)))
  }

  async function submit(event: FormEvent) {
    event.preventDefault()
    const animal = animals.find((item) => item.id === animalId)
    if (!animal) {
      setError('Selecciona una mascota.')
      return
    }
    setSubmitting(true)
    setError('')
    try {
      await createPost({
        shelterId: animal.shelterId,
        animalId: animal.id,
        caption,
        location: location || undefined,
        hashtags: hashtags || undefined,
        isFeatured,
        isSuccessStory,
        files,
      })
      setDone(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo publicar.')
    } finally {
      setSubmitting(false)
    }
  }

  const selectedAnimal = animals.find((item) => item.id === animalId)
  const selectedAnimalThumb =
    selectedAnimal?.media.find((media) => media.isPrimary)?.thumbnailUrl ??
    selectedAnimal?.media[0]?.thumbnailUrl ??
    null

  if (done) {
    return (
      <div>
        <p className="body-copy">¡Publicación creada!</p>
        <button className="primary-button" onClick={() => navigate('/')}>
          Ver el muro
        </button>
      </div>
    )
  }

  return (
    <div>
      <div className="admin-header">
        <div className="admin-header-title">
          <span className="admin-header-icon">📸</span>
          <div>
            <p className="eyebrow">Panel admin</p>
            <h1>Nueva publicación</h1>
          </div>
        </div>
      </div>

      {!loadingAnimals && animals.length === 0 ? (
        <div className="admin-empty-card glass-card">
          <span className="admin-empty-icon">🐾</span>
          <h2>Todavía no tienes mascotas registradas</h2>
          <p className="body-copy">Registra tu primera mascota para poder publicar sobre ella.</p>
          <button className="primary-button" onClick={() => navigate('/animals/new')}>
            Registrar mascota
          </button>
        </div>
      ) : (
        <form className="register-form" onSubmit={submit}>
          <label>
            Mascota
            <select value={animalId} onChange={(event) => setAnimalId(event.target.value)} required>
              <option value="">Selecciona una mascota</option>
              {animals.map((animal) => (
                <option key={animal.id} value={animal.id}>
                  {animal.name}
                </option>
              ))}
            </select>
          </label>

          {selectedAnimal && (
            <div className="selected-animal-chip">
              {selectedAnimalThumb ? (
                <img src={selectedAnimalThumb} alt={selectedAnimal.name} />
              ) : (
                <span>🐾</span>
              )}
              {selectedAnimal.name}
            </div>
          )}

          <div className="upload-field">
            <span className="field-label">Fotos/video</span>
            <label htmlFor="post-media" className="upload-box">
              ＋<strong>Agregar fotos o video</strong>
              <small>JPG, PNG, WEBP o MP4</small>
            </label>
            <input
              id="post-media"
              type="file"
              multiple
              accept="image/*,video/mp4"
              hidden
              onChange={handleFilesChange}
            />
            {files.length > 0 && (
              <div className="gallery media-preview-grid">
                {files.map((file, index) =>
                  file.type.startsWith('image/') ? (
                    <img key={index} src={previews[index]} alt={file.name} />
                  ) : (
                    <div key={index} className="video-chip">
                      🎬<span>{file.name}</span>
                    </div>
                  ),
                )}
              </div>
            )}
          </div>

          <label>
            Descripción
            <textarea
              rows={4}
              value={caption}
              onChange={(event) => setCaption(event.target.value)}
              required
            />
          </label>
          <label>
            Ubicación (opcional)
            <input value={location} onChange={(event) => setLocation(event.target.value)} />
          </label>
          <label>
            Hashtags (opcional)
            <input
              value={hashtags}
              onChange={(event) => setHashtags(event.target.value)}
              placeholder="#adopcion #rescate"
            />
          </label>
          <label className="checkbox-row">
            <input
              type="checkbox"
              checked={isFeatured}
              onChange={(event) => setIsFeatured(event.target.checked)}
            />
            Destacar publicación
          </label>
          <label className="checkbox-row">
            <input
              type="checkbox"
              checked={isSuccessStory}
              onChange={(event) => setIsSuccessStory(event.target.checked)}
            />
            Es una historia de éxito (final feliz)
          </label>
          {error && (
            <p className="feedback" role="status">
              {error}
            </p>
          )}
          <button className="primary-button" type="submit" disabled={submitting}>
            {submitting ? 'Publicando…' : '📸 Publicar'}
          </button>
        </form>
      )}
    </div>
  )
}
