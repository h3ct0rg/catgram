import { FormEvent, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useSession } from '../../context/SessionContext'
import {
  deleteComment,
  getComments,
  likeComment,
  postComment,
  unlikeComment,
} from '../../services/apiClient'
import { Comment } from '../../types/social'
import { formatRelativeTime } from '../../utils/formatRelativeTime'
import { getInitials, stringToColor } from '../../utils/avatarColor'

type Props = {
  postId: string
  onClose: () => void
  onCommentCountChange?: (delta: number) => void
}

export function CommentSheet({ postId, onClose, onCommentCountChange }: Props) {
  const session = useSession()
  const navigate = useNavigate()
  const [comments, setComments] = useState<Comment[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [body, setBody] = useState('')
  const [replyTo, setReplyTo] = useState<Comment | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    let cancelled = false
    getComments(postId)
      .then((items) => {
        if (!cancelled) setComments(items)
      })
      .catch(() => {
        if (!cancelled) setError('No se pudieron cargar los comentarios.')
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [postId])

  const topLevel = comments.filter((comment) => !comment.parentCommentId)
  const repliesOf = (id: string) => comments.filter((comment) => comment.parentCommentId === id)

  async function submit(event: FormEvent) {
    event.preventDefault()
    if (!body.trim()) return
    setSubmitting(true)
    setError('')
    try {
      const created = await postComment(postId, body.trim(), replyTo?.id)
      setComments((current) => [...current, created])
      setBody('')
      setReplyTo(null)
      onCommentCountChange?.(1)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo publicar el comentario.')
    } finally {
      setSubmitting(false)
    }
  }

  async function remove(comment: Comment) {
    try {
      await deleteComment(comment.id)
      setComments((current) =>
        current.filter((item) => item.id !== comment.id && item.parentCommentId !== comment.id),
      )
      onCommentCountChange?.(-1)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo eliminar el comentario.')
    }
  }

  async function toggleCommentLike(comment: Comment) {
    if (!session.isAuthenticated) {
      navigate('/login')
      return
    }
    const next = !comment.likedByCurrentUser
    setComments((current) =>
      current.map((item) =>
        item.id === comment.id
          ? { ...item, likedByCurrentUser: next, likeCount: item.likeCount + (next ? 1 : -1) }
          : item,
      ),
    )
    try {
      if (next) await likeComment(comment.id)
      else await unlikeComment(comment.id)
    } catch {
      setComments((current) =>
        current.map((item) =>
          item.id === comment.id
            ? { ...item, likedByCurrentUser: !next, likeCount: item.likeCount + (next ? -1 : 1) }
            : item,
        ),
      )
    }
  }

  function CommentRow({ comment, isReply }: { comment: Comment; isReply?: boolean }) {
    return (
      <div className={`comment-row ${isReply ? 'reply' : ''}`}>
        <span className="comment-avatar" style={{ background: stringToColor(comment.authorId) }}>
          {getInitials(comment.authorName)}
        </span>
        <div className="comment-body-col">
          <div className="comment-bubble">
            <strong>{comment.authorName}</strong>
            <p>{comment.body}</p>
          </div>
          <div className="comment-meta-row">
            <button
              className={`comment-like ${comment.likedByCurrentUser ? 'liked' : ''}`}
              onClick={() => toggleCommentLike(comment)}
            >
              Me gusta{comment.likeCount > 0 && ` · ${comment.likeCount}`}
            </button>
            {session.isAuthenticated && !isReply && (
              <button className="comment-reply-trigger" onClick={() => setReplyTo(comment)}>
                Responder
              </button>
            )}
            <span className="comment-time">{formatRelativeTime(comment.createdAt)}</span>
            {comment.isMine && (
              <button className="comment-delete-trigger" onClick={() => remove(comment)}>
                Eliminar
              </button>
            )}
          </div>
        </div>
      </div>
    )
  }

  return (
    <section className="inline-comments" onClick={(event) => event.stopPropagation()}>
      <div className="inline-comments-header">
        <h3>Comentarios</h3>
        <button className="icon-button" aria-label="Ocultar comentarios" onClick={onClose}>
          <span className="material-symbols-outlined">expand_less</span>
        </button>
      </div>

      <div className="comment-list">
        {loading && <p className="body-copy">Cargando comentarios…</p>}
        {!loading && topLevel.length === 0 && (
          <p className="body-copy comment-empty">Sé el primero en comentar 🐾</p>
        )}
        {topLevel.map((comment) => (
          <div className="comment-thread" key={comment.id}>
            <CommentRow comment={comment} />
            {repliesOf(comment.id).map((reply) => (
              <CommentRow comment={reply} isReply key={reply.id} />
            ))}
          </div>
        ))}
      </div>

      {error && (
        <p className="feedback" role="status">
          {error}
        </p>
      )}

      {session.isAuthenticated ? (
        <form className="comment-form" onSubmit={submit}>
          {replyTo && (
            <small className="replying-to">
              Respondiendo a un comentario
              <button type="button" onClick={() => setReplyTo(null)}>
                ✕
              </button>
            </small>
          )}
          <span
            className="comment-avatar comment-avatar-self"
            style={{ background: stringToColor(session.userId ?? session.userName ?? '') }}
          >
            {getInitials(session.userName ?? '?')}
          </span>
          <input
            value={body}
            onChange={(event) => setBody(event.target.value)}
            placeholder="Escribe un comentario…"
          />
          <button
            type="submit"
            className={`comment-send ${body.trim() ? 'ready' : ''}`}
            aria-label="Enviar comentario"
            disabled={submitting || !body.trim()}
          >
            <span className="material-symbols-outlined">send</span>
          </button>
        </form>
      ) : (
        <p className="body-copy">
          <button className="link-button" onClick={() => navigate('/login')}>
            Inicia sesión
          </button>{' '}
          para comentar.
        </p>
      )}
    </section>
  )
}
