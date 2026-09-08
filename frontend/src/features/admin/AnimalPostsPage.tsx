import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { deletePost, getAnimal, getAnimalPosts } from '../../services/apiClient'
import { Animal, Post } from '../../types/domain'
import { formatRelativeTime } from '../../utils/formatRelativeTime'
import { ConfirmModal } from './ConfirmModal'

export function AnimalPostsPage() {
  const { animalId } = useParams<{ animalId: string }>()
  const navigate = useNavigate()
  const [animal, setAnimal] = useState<Animal | null>(null)
  const [posts, setPosts] = useState<Post[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [busyId, setBusyId] = useState('')
  const [postToDelete, setPostToDelete] = useState<Post | null>(null)
  const [deleteError, setDeleteError] = useState('')

  useEffect(() => {
    if (!animalId) return
    let cancelled = false
    Promise.all([getAnimal(animalId), getAnimalPosts(animalId)])
      .then(([animalResult, postsResult]) => {
        if (cancelled) return
        setAnimal(animalResult)
        setPosts(postsResult)
      })
      .catch(() => {
        if (!cancelled) setError('No se pudieron cargar las publicaciones.')
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [animalId])

  async function confirmDelete() {
    if (!postToDelete) return
    setBusyId(postToDelete.id)
    setDeleteError('')
    try {
      await deletePost(postToDelete.id)
      setPosts((current) => current.filter((item) => item.id !== postToDelete.id))
      setPostToDelete(null)
    } catch (err) {
      setDeleteError(err instanceof Error ? err.message : 'No se pudo eliminar la publicación.')
    } finally {
      setBusyId('')
    }
  }

  const thumb =
    animal?.media.find((media) => media.isPrimary)?.thumbnailUrl ?? animal?.media[0]?.thumbnailUrl

  return (
    <div>
      <button className="back-button" onClick={() => navigate('/admin/pets')}>
        ‹ Volver
      </button>
      <div className="admin-header">
        <div className="admin-header-title">
          {thumb ? (
            <img src={thumb} alt={animal?.name} className="admin-row-thumb" />
          ) : (
            <span className="admin-header-icon">🐾</span>
          )}
          <div>
            <p className="eyebrow">Panel admin</p>
            <h1>Publicaciones de {animal?.name ?? '…'}</h1>
          </div>
        </div>
        <div className="admin-header-action">
          <button
            className="primary-button"
            type="button"
            onClick={() => navigate(`/admin/posts/new?animalId=${animalId}`)}
          >
            Nueva publicación
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
        {posts.map((post) => {
          const postThumb =
            post.media.find((media) => media.isPrimary)?.thumbnailUrl ?? post.media[0]?.thumbnailUrl
          return (
            <div className="admin-row" key={post.id}>
              <div className="admin-row-pet">
                {postThumb ? (
                  <img src={postThumb} alt={post.caption} className="admin-row-thumb" />
                ) : (
                  <span className="admin-row-thumb-fallback">📸</span>
                )}
                <div>
                  <strong>
                    {post.caption.length > 60 ? `${post.caption.slice(0, 60)}…` : post.caption}
                  </strong>
                  <br />
                  <small>
                    {formatRelativeTime(post.createdAt)} · ❤ {post.likeCount} · 💬{' '}
                    {post.commentCount}
                  </small>
                </div>
              </div>
              <div className="admin-row-actions">
                <button
                  className="secondary-button"
                  onClick={() => navigate(`/admin/pets/${animalId}/posts/${post.id}`)}
                >
                  Ver
                </button>
                <button
                  className="danger-button"
                  disabled={busyId === post.id}
                  onClick={() => {
                    setDeleteError('')
                    setPostToDelete(post)
                  }}
                >
                  Eliminar
                </button>
              </div>
            </div>
          )
        })}
        {!loading && posts.length === 0 && (
          <p className="admin-empty">Esta mascota todavía no tiene publicaciones.</p>
        )}
      </div>

      {postToDelete && (
        <ConfirmModal
          title="Eliminar publicación"
          message="¿Seguro que quieres eliminar esta publicación? Esta acción no se puede deshacer."
          confirmLabel="Eliminar"
          danger
          busy={busyId === postToDelete.id}
          error={deleteError}
          onConfirm={confirmDelete}
          onCancel={() => setPostToDelete(null)}
        />
      )}
    </div>
  )
}
