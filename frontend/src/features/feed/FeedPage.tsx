import { useEffect, useRef, useState } from 'react'
import { BottomNav } from '../../components/layout/BottomNav'
import { TopBar } from '../../components/layout/TopBar'
import { getFeed } from '../../services/apiClient'
import { Post } from '../../types/domain'
import { PostCard } from './PostCard'
import { fallbackPosts, stories } from './feedData'
import { StoryRail } from './StoryRail'

type Props = { onNavigate: (screen: string) => void }

export function FeedPage({ onNavigate }: Props) {
  const [posts, setPosts] = useState<Post[]>(fallbackPosts)
  const [sort, setSort] = useState('recent')
  const [loading, setLoading] = useState(false)
  const [hasMore, setHasMore] = useState(true)
  const sentinel = useRef<HTMLDivElement>(null)

  useEffect(() => {
    getFeed().then((items) => {
      if (items.length) setPosts(items)
    }).catch(() => undefined)
  }, [])

  useEffect(() => {
    if (!sentinel.current) return
    const observer = new IntersectionObserver((entries) => {
      if (!entries[0].isIntersecting || !hasMore || loading) return
      setLoading(true)
      window.setTimeout(() => {
        setPosts((current) => [...current, ...current.slice(0, 2).map((post, index) => ({ ...post, id: `${post.id}-${current.length}-${index}` }))])
        setHasMore(false)
        setLoading(false)
      }, 600)
    }, { rootMargin: '300px' })
    observer.observe(sentinel.current)
    return () => observer.disconnect()
  }, [hasMore, loading])

  const ordered = sort === 'popular' ? [...posts].sort((a, b) => b.likes - a.likes) : posts

  return (
    <div className="app-shell">
      <TopBar onHome={() => onNavigate('feed')} onProfile={() => onNavigate('login')} />
      <main className="feed-page">
        <div className="feed-heading">
          <div><p className="eyebrow">Un hogar empieza aquí</p><h1>Descubre historias</h1></div>
          <select value={sort} onChange={(event) => setSort(event.target.value)} aria-label="Ordenar publicaciones">
            <option value="recent">Más recientes</option><option value="popular">Más populares</option>
          </select>
        </div>
        <StoryRail stories={stories} onAdd={() => onNavigate('register')} onOpen={() => onNavigate('pet')} />
        <div className="post-list">{ordered.map((post) => <PostCard post={post} key={post.id} onOpen={() => onNavigate('pet')} />)}</div>
        <div ref={sentinel} className="feed-end">{loading ? 'Cargando más historias…' : hasMore ? '' : 'Has visto todas las historias por ahora 🐾'}</div>
      </main>
      <BottomNav onHome={() => onNavigate('feed')} onSearch={() => onNavigate('search')} onCreate={() => onNavigate('register')} onProfile={() => onNavigate('login')} />
    </div>
  )
}
