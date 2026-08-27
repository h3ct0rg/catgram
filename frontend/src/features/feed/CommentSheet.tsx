import { FormEvent, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useSession } from '../../context/SessionContext'
import { deleteComment, getComments, postComment } from '../../services/apiClient'
import { Comment } from '../../types/social'

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

  return (
    <div className="sheet-overlay" role="dialog" aria-modal="true" onClick={onClose}>
      <div className="sheet comment-sheet" onClick={(event) => event.stopPropagation()}>
        <div className="sheet-handle" />
        <h2>Comentarios</h2>
        <div className="comment-list">
          {loading && <p className="body-copy">Cargando comentarios…</p>}
          {!loading && topLevel.length === 0 && (
            <p className="body-copy">Sé el primero en comentar 🐾</p>
          )}
          {topLevel.map((comment) => (
            <div className="comment" key={comment.id}>
              <p>{comment.body}</p>
              <div className="comment-actions">
                {session.isAuthenticated && (
                  <button onClick={() => setReplyTo(comment)}>Responder</button>
                )}
                {comment.isMine && <button onClick={() => remove(comment)}>Eliminar</button>}
              </div>
              {repliesOf(comment.id).map((reply) => (
                <div className="comment reply" key={reply.id}>
                  <p>{reply.body}</p>
                  {reply.isMine && (
                    <div className="comment-actions">
                      <button onClick={() => remove(reply)}>Eliminar</button>
                    </div>
                  )}
                </div>
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
            <input
              value={body}
              onChange={(event) => setBody(event.target.value)}
              placeholder="Escribe un comentario…"
            />
            <button className="primary-button" disabled={submitting || !body.trim()}>
              Enviar
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
        <button className="secondary-button" onClick={onClose}>
          Cerrar
        </button>
      </div>
    </div>
  )
}
