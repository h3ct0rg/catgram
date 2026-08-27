import { useEffect, useState } from 'react'
import { useSession } from '../../context/SessionContext'
import { follow, getFollowSummary, unfollow } from '../../services/apiClient'
import { FollowSummary } from '../../types/social'

type Props = { animalId: string }

export function FollowButton({ animalId }: Props) {
  const session = useSession()
  const [summary, setSummary] = useState<FollowSummary | null>(null)
  const [pending, setPending] = useState(false)

  useEffect(() => {
    let cancelled = false
    getFollowSummary(animalId)
      .then((result) => {
        if (!cancelled) setSummary(result)
      })
      .catch(() => undefined)
    return () => {
      cancelled = true
    }
  }, [animalId])

  if (!session.isAuthenticated || !summary) return null

  async function toggle() {
    if (!summary || pending) return
    setPending(true)
    const next: FollowSummary = {
      followedByCurrentUser: !summary.followedByCurrentUser,
      followerCount: summary.followerCount + (summary.followedByCurrentUser ? -1 : 1),
    }
    setSummary(next)
    try {
      if (next.followedByCurrentUser) await follow(animalId)
      else await unfollow(animalId)
    } catch {
      setSummary(summary)
    } finally {
      setPending(false)
    }
  }

  return (
    <button className="secondary-button follow-button" onClick={toggle} disabled={pending}>
      {summary.followedByCurrentUser
        ? `✓ Siguiendo (${summary.followerCount})`
        : `+ Seguir (${summary.followerCount})`}
    </button>
  )
}
