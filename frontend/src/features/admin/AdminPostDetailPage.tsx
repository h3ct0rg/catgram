import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { MediaCarousel } from '../../components/media/MediaCarousel'
import { getPostForAdmin } from '../../services/apiClient'
import { Post } from '../../types/domain'
import { formatRelativeTime } from '../../utils/formatRelativeTime'

export function AdminPostDetailPage() {
  const { animalId, postId } = useParams<{ animalId: string; postId: string }>()
  const navigate = useNavigate()
  const [post, setPost] = useState<Post | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!postId) return
    let cancelled = false
    getPostForAdmin(postId)
      .then((result) => {
        if (!cancelled) setPost(result)
      })
      .catch(() => {
        if (!cancelled) setError('No se pudo cargar la publicación.')
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [postId])

  return (
    <div>
      <button className="back-button" onClick={() => navigate(`/admin/pets/${animalId}/posts`)}>
        ‹ Volver
      </button>

      {loading && <p className="body-copy">Cargando…</p>}
      {error && (
        <p className="feedback" role="status">
          {error}
        </p>
      )}

      {post && (
        <>
          <div className="admin-header">
            <div className="admin-header-title">
              <span className="admin-header-icon">📸</span>
              <div>
                <p className="eyebrow">Panel admin</p>
                <h1>Publicación de {post.animalName}</h1>
              </div>
            </div>
          </div>

          {post.media.length > 0 && (
            <div className="post-media">
              <MediaCarousel media={post.media} className="post-image-wrap" />
            </div>
          )}

          <section className="glass-card about">
            <h2>
              <span className="material-symbols-outlined">description</span> Descripción
            </h2>
            <p>{post.caption}</p>
            <small className="timestamp">{formatRelativeTime(post.createdAt)}</small>
          </section>

          <section className="glass-card animal-stats">
            <h2>📊 &nbsp;Estadísticas</h2>
            <div className="admin-grid">
              <div className="admin-tile">
                <strong>{post.viewCount}</strong>
                <span>Vistas</span>
              </div>
              <div className="admin-tile">
                <strong>{post.likeCount}</strong>
                <span>Likes</span>
              </div>
              <div className="admin-tile">
                <strong>{post.commentCount}</strong>
                <span>Comentarios</span>
              </div>
              <div className="admin-tile">
                <strong>{post.shareCount}</strong>
                <span>Compartidos</span>
              </div>
            </div>
          </section>
        </>
      )}
    </div>
  )
}
