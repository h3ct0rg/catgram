import { Post } from '../../types/domain'

type Props = {
  post: Post
  onOpen: () => void
}

export function PostCard({ post, onOpen }: Props) {
  return (
    <article className="post-card">
      <div className="post-meta">
        <span className="shelter-icon">🏠</span>
        <div>
          <strong>{post.shelter}</strong>
          <small>📍 Comunidad Kindred Paws</small>
        </div>
        <button className="more" aria-label="Más opciones">•••</button>
      </div>
      <button className="post-image-wrap image-button" onClick={onOpen}>
        <img src={post.image} alt={`${post.animal}, disponible para adopción`} />
        <span className="status">✓ {post.status}</span>
      </button>
      <div className="post-actions">
        <button>♡ <span>{post.likes}</span></button>
        <button>▢ <span>{post.comments}</span></button>
        <button>⌁</button>
        <button className="save">♧</button>
      </div>
      <p className="caption"><strong>{post.shelter}</strong> {post.caption}</p>
      <small className="timestamp">{post.createdAt}</small>
    </article>
  )
}
