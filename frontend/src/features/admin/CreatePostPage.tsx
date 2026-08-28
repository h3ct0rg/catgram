import { FormEvent, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useSession } from '../../context/SessionContext'
import { createPost, getAnimals } from '../../services/apiClient'
import { Animal } from '../../types/domain'

export function CreatePostPage() {
  const navigate = useNavigate()
  const session = useSession()
  const [animals, setAnimals] = useState<Animal[]>([])
  const [animalId, setAnimalId] = useState('')
  const [caption, setCaption] = useState('')
  const [location, setLocation] = useState('')
  const [hashtags, setHashtags] = useState('')
  const [isFeatured, setIsFeatured] = useState(false)
  const [isSuccessStory, setIsSuccessStory] = useState(false)
  const [files, setFiles] = useState<File[]>([])
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')
  const [done, setDone] = useState(false)

  useEffect(() => {
    getAnimals(session.shelterId ? { shelterId: session.shelterId } : {})
      .then(setAnimals)
      .catch(() => undefined)
  }, [session.shelterId])

  async function submit(event: FormEvent) {
    event.preventDefault()
    const animal = animals.find((item) => item.id === animalId)
    if (!animal) {
      setError('Selecciona un animal.')
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
    <form className="register-form" onSubmit={submit}>
      <label>
        Animal
        <select value={animalId} onChange={(event) => setAnimalId(event.target.value)} required>
          <option value="">Selecciona un animal</option>
          {animals.map((animal) => (
            <option key={animal.id} value={animal.id}>
              {animal.name}
            </option>
          ))}
        </select>
      </label>
      <label>
        Fotos/video
        <input
          type="file"
          multiple
          accept="image/*,video/mp4"
          onChange={(event) => setFiles(Array.from(event.target.files ?? []))}
        />
      </label>
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
  )
}
