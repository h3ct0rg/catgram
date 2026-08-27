import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { getStories, viewStory } from '../../services/apiClient'
import { Story } from '../../types/domain'

const DURATION_MS = 5000
const ANONYMOUS_KEY_STORAGE = 'kindred_paws_anonymous_key'

function getAnonymousKey(): string {
  let key = localStorage.getItem(ANONYMOUS_KEY_STORAGE)
  if (!key) {
    key = crypto.randomUUID()
    localStorage.setItem(ANONYMOUS_KEY_STORAGE, key)
  }
  return key
}

export function StoryViewer() {
  const { storyId } = useParams<{ storyId: string }>()
  const navigate = useNavigate()
  const [stories, setStories] = useState<Story[]>([])
  const [loading, setLoading] = useState(true)
  const [progress, setProgress] = useState(0)

  useEffect(() => {
    getStories()
      .then(setStories)
      .catch(() => undefined)
      .finally(() => setLoading(false))
  }, [])

  const index = useMemo(
    () => stories.findIndex((story) => story.id === storyId),
    [stories, storyId],
  )
  const current = index >= 0 ? stories[index] : null

  function goNext() {
    if (index >= 0 && index < stories.length - 1)
      navigate(`/stories/${stories[index + 1].id}`, { replace: true })
    else navigate('/')
  }

  function goPrev() {
    if (index > 0) navigate(`/stories/${stories[index - 1].id}`, { replace: true })
  }

  useEffect(() => {
    if (!current) return
    viewStory(current.id, getAnonymousKey()).catch(() => undefined)
    setProgress(0)
    const start = Date.now()
    const interval = window.setInterval(() => {
      const elapsed = Date.now() - start
      const ratio = Math.min(1, elapsed / DURATION_MS)
      setProgress(ratio)
      if (ratio >= 1) goNext()
    }, 100)
    return () => window.clearInterval(interval)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [current?.id])

  if (loading) {
    return (
      <main className="story-viewer">
        <p className="body-copy">Cargando…</p>
      </main>
    )
  }

  if (!current) {
    return (
      <main className="story-viewer story-viewer-empty">
        <p className="body-copy">Esta historia ya no está disponible.</p>
        <button className="primary-button" onClick={() => navigate('/')}>
          Volver al muro
        </button>
      </main>
    )
  }

  return (
    <main className="story-viewer">
      <div className="story-progress">
        {stories.map((story, i) => (
          <div className="story-progress-track" key={story.id}>
            <div
              className="story-progress-fill"
              style={{ width: `${i < index ? 100 : i === index ? progress * 100 : 0}%` }}
            />
          </div>
        ))}
      </div>
      <button className="story-close" onClick={() => navigate('/')} aria-label="Cerrar">
        ✕
      </button>
      <div className="story-media">
        <img src={current.mediaUrl} alt={current.animalName} />
        <div className="story-caption">
          <strong>{current.animalName}</strong>
          <p>{current.caption}</p>
        </div>
      </div>
      <button
        className="story-tap story-tap-prev"
        aria-label="Historia anterior"
        onClick={goPrev}
      />
      <button
        className="story-tap story-tap-next"
        aria-label="Historia siguiente"
        onClick={goNext}
      />
    </main>
  )
}
