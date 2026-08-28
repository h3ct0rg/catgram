import { Link, useNavigate } from 'react-router-dom'
import { Story } from '../../types/domain'
import { useSession } from '../../context/SessionContext'

type Props = { stories: Story[] }

export function StoryRail({ stories }: Props) {
  const navigate = useNavigate()
  const session = useSession()

  return (
    <div className="stories" aria-label="Historias">
      <button
        type="button"
        className="story story-add"
        onClick={() => (session.isAuthenticated ? navigate('/animals/new') : navigate('/login'))}
      >
        <span className="story-ring story-add-ring">
          <span className="story-add-icon material-symbols-outlined">add</span>
        </span>
        <small>Tu historia</small>
      </button>

      {stories.map((story) => (
        <Link className="story" to={`/stories/${story.id}`} key={story.id}>
          <span className="story-ring">
            <img src={story.mediaUrl} alt={story.animalName} />
          </span>
          <small>{story.animalName}</small>
        </Link>
      ))}
    </div>
  )
}

