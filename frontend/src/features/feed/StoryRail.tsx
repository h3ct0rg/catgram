import { Story } from '../../types/domain'

type Props = {
  stories: Story[]
  onAdd: () => void
  onOpen: () => void
}

export function StoryRail({ stories, onAdd, onOpen }: Props) {
  return (
    <div className="stories" aria-label="Historias">
      <button className="story add-story" onClick={onAdd}>
        <span>＋</span>
        <small>Publicar</small>
      </button>
      {stories.map((story) => (
        <button className={`story ${story.viewed ? 'viewed' : ''}`} key={story.id} onClick={onOpen}>
          <img src={story.image} alt={story.name} />
          <small>{story.name}</small>
        </button>
      ))}
    </div>
  )
}
