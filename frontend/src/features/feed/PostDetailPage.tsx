import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { getPost } from '../../services/apiClient'
import { Post } from '../../types/domain'
import { PostCard } from './PostCard'

export function PostDetailPage() {
  const { postId } = useParams<{ postId: string }>()
  const navigate = useNavigate()
  const [post, setPost] = useState<Post | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!postId) return
    let cancelled = false
    setLoading(true)
    getPost(postId)
      .then((result) => {
        if (!cancelled) setPost(result)
      })
      .catch(() => {
        if (!cancelled) setError('No pudimos encontrar esta publicación.')
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [postId])

  return (
    <main className="feed-page post-detail-page">
      <button className="back-button" onClick={() => navigate('/')}>
        ‹ Volver
      </button>
      {loading && <p className="body-copy">Cargando publicación…</p>}
      {!loading && error && (
        <>
          <p className="body-copy">{error}</p>
          <button className="primary-button" onClick={() => navigate('/')}>
            Volver al muro
          </button>
        </>
      )}
      {!loading && post && <PostCard post={post} />}
    </main>
  )
}
