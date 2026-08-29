import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Story } from '../../types/domain'
import { useSession } from '../../context/SessionContext'
import { CreateStoryModal } from './CreateStoryModal'

type Props = {
  stories: Story[]
  onStoryAdded?: (story: Story) => void
}

const ADMIN_ROLES = ['Administrador', 'SuperAdministrador']

export function StoryRail({ stories, onStoryAdded }: Props) {
  const session = useSession()
  const isAdmin = session.roles.some((r) => ADMIN_ROLES.includes(r))
  const [showModal, setShowModal] = useState(false)

  function handlePublished(story: Story) {
    onStoryAdded?.(story)
  }

  return (
    <>
      <div className="stories" aria-label="Historias">
        {/* Only show "Tu historia" button to shelter admins */}
        {isAdmin && (
          <button
            type="button"
            className="story story-add"
            onClick={() => setShowModal(true)}
          >
            <span className="story-ring story-add-ring">
              <span className="story-add-icon material-symbols-outlined">add</span>
            </span>
            <small>Tu historia</small>
          </button>
        )}

        {stories.map((story) => (
          <Link className="story" to={`/stories/${story.id}`} key={story.id}>
            <span className="story-ring">
              <img src={story.mediaUrl} alt={story.animalName} />
            </span>
            <small>{story.animalName}</small>
          </Link>
        ))}
      </div>

      {showModal && (
        <CreateStoryModal
          onClose={() => setShowModal(false)}
          onPublished={handlePublished}
        />
      )}
    </>
  )
}
