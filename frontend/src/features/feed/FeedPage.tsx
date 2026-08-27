import { useCallback, useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { BottomNav } from '../../components/layout/BottomNav'
import { TopBar } from '../../components/layout/TopBar'
import { getFeed, getStories } from '../../services/apiClient'
import { Post, Story } from '../../types/domain'
import { PostCard } from './PostCard'
import { StoryRail } from './StoryRail'

type Sort = 'recent' | 'popular'

export function FeedPage() {
  const navigate = useNavigate()
  const [posts, setPosts] = useState<Post[]>([])
  const [stories, setStories] = useState<Story[]>([])
  const [sort, setSort] = useState<Sort>('recent')
  const [cursor, setCursor] = useState<string | null>(null)
  const [hasMore, setHasMore] = useState(true)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const sentinel = useRef<HTMLDivElement>(null)

  const loadPage = useCallback(
    async (currentSort: Sort, currentCursor: string | null, replace: boolean) => {
      setLoading(true)
      setError('')
      try {
        const page = await getFeed({ cursor: currentCursor, sort: currentSort, pageSize: 10 })
        setPosts((current) => (replace ? page.items : [...current, ...page.items]))
        setCursor(page.nextCursor)
        setHasMore(page.nextCursor !== null)
      } catch {
        setError('No pudimos cargar el muro. Intenta de nuevo.')
      } finally {
        setLoading(false)
      }
    },
    [],
  )

  useEffect(() => {
    loadPage(sort, null, true)
    getStories()
      .then(setStories)
      .catch(() => undefined)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [sort])

  useEffect(() => {
    if (!sentinel.current) return
    const observer = new IntersectionObserver(
      (entries) => {
        if (!entries[0].isIntersecting || !hasMore || loading) return
        loadPage(sort, cursor, false)
      },
      { rootMargin: '300px' },
    )
    observer.observe(sentinel.current)
    return () => observer.disconnect()
  }, [hasMore, loading, sort, cursor, loadPage])

  return (
    <div className="app-shell">
      <TopBar onHome={() => navigate('/')} />
      <main className="feed-page">
        <div className="feed-heading">
          <div>
            <p className="eyebrow">Un hogar empieza aquí</p>
            <h1>Descubre historias</h1>
          </div>
          <select
            value={sort}
            onChange={(event) => setSort(event.target.value as Sort)}
            aria-label="Ordenar publicaciones"
          >
            <option value="recent">Más recientes</option>
            <option value="popular">Más populares</option>
          </select>
        </div>
        <StoryRail stories={stories} />
        {error && (
          <p className="feedback" role="status">
            {error}
          </p>
        )}
        {!loading && !error && posts.length === 0 && (
          <p className="body-copy">Todavía no hay publicaciones. Vuelve pronto 🐾</p>
        )}
        <div className="post-list">
          {posts.map((post) => (
            <PostCard post={post} key={post.id} />
          ))}
        </div>
        <div ref={sentinel} className="feed-end">
          {loading
            ? 'Cargando más historias…'
            : hasMore
              ? ''
              : 'Has visto todas las historias por ahora 🐾'}
        </div>
      </main>
      <BottomNav
        onHome={() => navigate('/')}
        onSearch={() => undefined}
        onCreate={() => navigate('/animals/new')}
      />
    </div>
  )
}
