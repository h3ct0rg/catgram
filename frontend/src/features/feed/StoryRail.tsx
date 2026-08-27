import { Link } from 'react-router-dom'
import { Story } from '../../types/domain'

type Props = { stories: Story[] }

export function StoryRail({ stories }: Props) {
  return (
    <div className="stories" aria-label="Historias">
      <Link className="story add-story" to="/animals/new">
        <span>＋</span>
        <small>Publicar</small>
      </Link>
      {stories.map((story) => (
        <Link className="story" to={`/stories/${story.id}`} key={story.id}>
          <img src={story.mediaUrl} alt={story.animalName} />
          <small>{story.animalName}</small>
        </Link>
      ))}
    </div>
  )
}
