import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useSession } from '../../context/SessionContext'
import { apiBaseUrl, like, unlike } from '../../services/apiClient'
import { Post } from '../../types/domain'
import {
  adoptionStatusColor,
  adoptionStatusIcon,
  adoptionStatusLabel,
} from '../../utils/adoptionStatus'
import { formatRelativeTime } from '../../utils/formatRelativeTime'
import { MediaCarousel } from '../../components/media/MediaCarousel'
import { ReportModal } from '../social/ReportModal'
import { CommentSheet } from './CommentSheet'
import { ShareSheet } from './ShareSheet'

type Props = { post: Post }

export function PostCard({ post }: Props) {
  const session = useSession()
  const navigate = useNavigate()
  const [liked, setLiked] = useState(post.likedByCurrentUser)
  const [likeCount, setLikeCount] = useState(post.likeCount)
  const [commentCount, setCommentCount] = useState(post.commentCount)
  const [pendingLike, setPendingLike] = useState(false)
  const [showComments, setShowComments] = useState(false)
  const [showShare, setShowShare] = useState(false)
  const [showReport, setShowReport] = useState(false)
  const [showMenu, setShowMenu] = useState(false)

  const shareUrl = `${apiBaseUrl}/p/${post.id}`
  async function toggleLike() {
    if (!session.isAuthenticated) {
      navigate('/login')
      return
    }
    if (pendingLike) return
    setPendingLike(true)
    const next = !liked
    setLiked(next)
    setLikeCount((count) => count + (next ? 1 : -1))
    try {
      if (next) await like(post.id)
      else await unlike(post.id)
    } catch {
      setLiked(!next)
      setLikeCount((count) => count + (next ? -1 : 1))
    } finally {
      setPendingLike(false)
    }
  }

  function openComments() {
    if (!session.isAuthenticated) {
      navigate('/login')
      return
    }
    setShowComments(true)
  }

  const avatarThumb =
    post.media.find((media) => media.isPrimary)?.thumbnailUrl ?? post.media[0]?.thumbnailUrl

  return (
    <article className="post-card">
      <div className="post-meta">
        <span className="shelter-icon">
          {avatarThumb ? (
            <img src={avatarThumb} alt={post.animalName} />
          ) : (
            <span className="material-symbols-outlined">storefront</span>
          )}
        </span>
        <Link to={`/animals/${post.animalId}`} className="post-meta-link">
          <strong>{post.shelterName}</strong>
          <small>📍 {post.animalName}</small>
        </Link>
        <button
          className="more"
          aria-label="Más opciones"
          onClick={() => setShowMenu((value) => !value)}
        >
          <span className="material-symbols-outlined">more_horiz</span>
        </button>
        {showMenu && (
          <div className="post-menu">
            <button
              onClick={() => {
                setShowMenu(false)
                setShowReport(true)
              }}
            >
              Reportar publicación
            </button>
          </div>
        )}
      </div>
      <div className="post-media">
        <MediaCarousel
          media={post.media}
          className="post-image-wrap"
          onOpen={() => navigate(`/p/${post.id}`)}
        />
        <span className="status" style={{ background: adoptionStatusColor(post.adoptionStatus) }}>
          {adoptionStatusIcon(post.adoptionStatus)} {adoptionStatusLabel(post.adoptionStatus)}
        </span>
      </div>
      <div className="post-actions">
        <button onClick={toggleLike} className={liked ? 'liked' : ''} aria-pressed={liked}>
          <span
            className="material-symbols-outlined"
            style={{ fontVariationSettings: `'FILL' ${liked ? 1 : 0}` }}
          >
            favorite
          </span>
          <span>{likeCount}</span>
        </button>
        <button onClick={openComments}>
          <span className="material-symbols-outlined">chat_bubble</span>
          <span>{commentCount}</span>
        </button>
        <button onClick={() => setShowShare(true)}>
          <span className="material-symbols-outlined">send</span>
        </button>
      </div>
      <p className="caption">
        <strong>{post.shelterName}</strong> {post.caption}
      </p>
      <small className="timestamp">{formatRelativeTime(post.createdAt)}</small>

      {showComments && (
        <CommentSheet
          postId={post.id}
          onClose={() => setShowComments(false)}
          onCommentCountChange={(delta) => setCommentCount((count) => count + delta)}
        />
      )}
      {showShare && (
        <ShareSheet
          url={shareUrl}
          title={post.animalName}
          text={`Ayuda a ${post.animalName} a encontrar un hogar`}
          postId={post.id}
          onClose={() => setShowShare(false)}
        />
      )}
      {showReport && (
        <ReportModal targetType="Post" targetId={post.id} onClose={() => setShowReport(false)} />
      )}
    </article>
  )
}
