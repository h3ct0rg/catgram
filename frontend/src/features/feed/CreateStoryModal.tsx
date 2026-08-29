import { ChangeEvent, FormEvent, useEffect, useRef, useState } from 'react'
import { getAnimals, createStory } from '../../services/apiClient'
import { Animal, Story } from '../../types/domain'
import { useSession } from '../../context/SessionContext'

type Props = {
  onClose: () => void
  onPublished: (story: Story) => void
}

const ACCEPT = 'image/*,video/mp4,video/webm,video/quicktime'

export function CreateStoryModal({ onClose, onPublished }: Props) {
  const session = useSession()
  const fileInputRef = useRef<HTMLInputElement>(null)

  const [animals, setAnimals] = useState<Animal[]>([])
  const [loadingAnimals, setLoadingAnimals] = useState(true)
  const [animalId, setAnimalId] = useState('')
  const [caption, setCaption] = useState('')
  const [file, setFile] = useState<File | null>(null)
  const [preview, setPreview] = useState<string | null>(null)
  const [isVideo, setIsVideo] = useState(false)
  const [publishing, setPublishing] = useState(false)
  const [error, setError] = useState('')

  // Load only shelter's animals
  useEffect(() => {
    const params = session.shelterId ? { shelterId: session.shelterId } : {}
    getAnimals(params)
      .then(setAnimals)
      .catch(() => setAnimals([]))
      .finally(() => setLoadingAnimals(false))
  }, [session.shelterId])

  function handleFileChange(e: ChangeEvent<HTMLInputElement>) {
    const selected = e.target.files?.[0]
    if (!selected) return
    setFile(selected)
    setIsVideo(selected.type.startsWith('video/'))
    const url = URL.createObjectURL(selected)
    setPreview(url)
  }

  function clearFile() {
    setFile(null)
    setPreview(null)
    setIsVideo(false)
    if (fileInputRef.current) fileInputRef.current.value = ''
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!animalId) { setError('Selecciona una mascota.'); return }
    if (!file) { setError('Elige una foto o video.'); return }
    setError('')
    setPublishing(true)
    try {
      const story = await createStory({ animalId, caption, file })
      onPublished(story)
      onClose()
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Error al publicar la historia.')
    } finally {
      setPublishing(false)
    }
  }

  return (
    <div className="modal-backdrop" role="dialog" aria-modal="true">
      <div className="create-story-modal glass-card">
        {/* Header */}
        <div className="create-story-header">
          <div className="create-story-header-left">
            <span className="material-symbols-outlined create-story-icon">auto_stories</span>
            <div>
              <h2>Nueva Historia</h2>
              <p>Visible para quienes siguen a tu mascota</p>
            </div>
          </div>
          <button type="button" className="modal-close-btn" onClick={onClose} aria-label="Cerrar">
            <span className="material-symbols-outlined">close</span>
          </button>
        </div>

        <form onSubmit={handleSubmit} className="create-story-form">
          {/* Animal selector */}
          <div className="create-story-field">
            <label htmlFor="story-animal">
              <span className="material-symbols-outlined">pets</span>
              ¿De qué mascota es esta historia?
            </label>
            {loadingAnimals ? (
              <p className="create-story-loading">Cargando tus mascotas…</p>
            ) : animals.length === 0 ? (
              <p className="create-story-empty-hint">
                No tienes mascotas registradas en tu refugio todavía.
              </p>
            ) : (
              <div className="animal-select-grid">
                {animals.map((animal) => (
                  <button
                    key={animal.id}
                    type="button"
                    className={`animal-select-card ${animalId === animal.id ? 'selected' : ''}`}
                    onClick={() => setAnimalId(animal.id)}
                  >
                    {animal.media?.[0] ? (
                      <img src={animal.media[0].url} alt={animal.name} />
                    ) : (
                      <span className="animal-select-placeholder material-symbols-outlined">pets</span>
                    )}
                    <span>{animal.name}</span>
                    {animalId === animal.id && (
                      <span className="animal-selected-check material-symbols-outlined">check_circle</span>
                    )}
                  </button>
                ))}
              </div>
            )}
          </div>

          {/* Media picker */}
          <div className="create-story-field">
            <label>
              <span className="material-symbols-outlined">perm_media</span>
              Foto o Video
            </label>

            {!preview ? (
              <button
                type="button"
                className="media-drop-zone"
                onClick={() => fileInputRef.current?.click()}
              >
                <span className="material-symbols-outlined media-drop-icon">add_photo_alternate</span>
                <span>Toca para elegir una foto o video</span>
                <small>JPG, PNG, MP4, hasta 50 MB</small>
              </button>
            ) : (
              <div className="story-preview-wrap">
                {isVideo ? (
                  <video src={preview} controls className="story-preview-media" />
                ) : (
                  <img src={preview} alt="Vista previa" className="story-preview-media" />
                )}
                <button type="button" className="story-preview-remove" onClick={clearFile}>
                  <span className="material-symbols-outlined">delete</span>
                </button>
              </div>
            )}

            <input
              ref={fileInputRef}
              type="file"
              accept={ACCEPT}
              onChange={handleFileChange}
              style={{ display: 'none' }}
            />
          </div>

          {/* Caption */}
          <div className="create-story-field">
            <label htmlFor="story-caption">
              <span className="material-symbols-outlined">edit_note</span>
              Texto (opcional)
            </label>
            <textarea
              id="story-caption"
              className="create-story-textarea"
              placeholder="Escribe algo sobre esta historia…"
              value={caption}
              onChange={(e) => setCaption(e.target.value)}
              maxLength={300}
              rows={3}
            />
            <small className="char-count">{caption.length}/300</small>
          </div>

          {error && (
            <p className="create-story-error" role="alert">
              <span className="material-symbols-outlined">error</span>
              {error}
            </p>
          )}

          {/* Actions */}
          <div className="create-story-actions">
            <button type="button" className="secondary-button" onClick={onClose} disabled={publishing}>
              Cancelar
            </button>
            <button
              type="submit"
              className="primary-button create-story-submit"
              disabled={publishing || !animalId || !file}
            >
              {publishing ? (
                <>
                  <span className="material-symbols-outlined rotating">progress_activity</span>
                  Publicando…
                </>
              ) : (
                <>
                  <span className="material-symbols-outlined">send</span>
                  Publicar Historia
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
