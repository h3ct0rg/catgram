import { useRef, useState } from 'react'
import { AnimalMedia } from '../../types/domain'

type Props = {
  media: AnimalMedia[]
  className?: string
  onOpen?: () => void
}

export function MediaCarousel({ media, className, onOpen }: Props) {
  const [index, setIndex] = useState(0)
  const touchStartX = useRef<number | null>(null)
  const dragged = useRef(false)

  if (media.length === 0) return null

  function goTo(next: number) {
    setIndex(Math.max(0, Math.min(next, media.length - 1)))
  }

  function handleTouchStart(event: React.TouchEvent) {
    touchStartX.current = event.touches[0].clientX
    dragged.current = false
  }

  function handleTouchMove(event: React.TouchEvent) {
    if (touchStartX.current === null) return
    if (Math.abs(event.touches[0].clientX - touchStartX.current) > 12) dragged.current = true
  }

  function handleTouchEnd(event: React.TouchEvent) {
    if (touchStartX.current === null) return
    const delta = event.changedTouches[0].clientX - touchStartX.current
    if (Math.abs(delta) > 40) goTo(delta < 0 ? index + 1 : index - 1)
    touchStartX.current = null
  }

  function handleClick() {
    if (dragged.current) return
    onOpen?.()
  }

  return (
    <div
      className={`media-carousel ${className ?? ''}`}
      onTouchStart={handleTouchStart}
      onTouchMove={handleTouchMove}
      onTouchEnd={handleTouchEnd}
      onClick={handleClick}
    >
      <div
        className="media-track"
        style={{ transform: `translateX(-${index * 100}%)` }}
      >
        {media.map((item) =>
          item.contentType.startsWith('video/') ? (
            <div className="media-slide" key={item.id}>
              <video
                src={item.url}
                controls
                playsInline
                preload="metadata"
                onClick={(event) => event.stopPropagation()}
              />
              <span className="video-badge">🎬</span>
            </div>
          ) : (
            <div className="media-slide" key={item.id}>
              <img src={item.thumbnailUrl ?? item.url} alt="" />
            </div>
          ),
        )}
      </div>
      {media.length > 1 && (
        <>
          <div className="carousel-dots">
            {media.map((item, i) => (
              <button
                type="button"
                key={item.id}
                className={`carousel-dot${i === index ? ' active' : ''}`}
                aria-label={`Ver media ${i + 1}`}
                onClick={(event) => {
                  event.stopPropagation()
                  goTo(i)
                }}
              />
            ))}
          </div>
          <span className="carousel-counter">
            {index + 1}/{media.length}
          </span>
        </>
      )}
    </div>
  )
}
