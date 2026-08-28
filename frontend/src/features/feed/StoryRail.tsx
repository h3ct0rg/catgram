import { Link } from 'react-router-dom'
import { Story } from '../../types/domain'

type Props = { stories: Story[] }

export function StoryRail({ stories }: Props) {
  if (stories.length === 0) return null

  return (
    <div className="stories" aria-label="Historias">
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
