import { useEffect, useRef, useState } from 'react'
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
  const containerRef = useRef<HTMLDivElement>(null)
  const [containerWidth, setContainerWidth] = useState(0)
  const [ratios, setRatios] = useState<Record<string, number>>({})

  useEffect(() => {
    const el = containerRef.current
    if (!el) return
    const observer = new ResizeObserver((entries) => {
      const width = entries[0]?.contentRect.width
      if (width) setContainerWidth(width)
    })
    observer.observe(el)
    return () => observer.disconnect()
  }, [])

  if (media.length === 0) return null

  function goTo(next: number) {
    setIndex(Math.max(0, Math.min(next, media.length - 1)))
  }

  function recordRatio(id: string, ratio: number) {
    setRatios((current) => (current[id] === ratio ? current : { ...current, [id]: ratio }))
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

  // The carousel's slides can each have a different aspect ratio (independent per-photo crop),
  // but they all share one flex row's height at any given time — so the container's own height
  // is driven by whichever slide is currently visible, and recalculated as the user swipes.
  const currentRatio = ratios[media[index]?.id]
  const height = containerWidth && currentRatio ? containerWidth / currentRatio : undefined

  return (
    <div
      ref={containerRef}
      className={`media-carousel ${className ?? ''}`}
      style={{ height }}
      onTouchStart={handleTouchStart}
      onTouchMove={handleTouchMove}
      onTouchEnd={handleTouchEnd}
      onClick={handleClick}
    >
      <div className="media-track" style={{ transform: `translateX(-${index * 100}%)` }}>
        {media.map((item) =>
          item.contentType.startsWith('video/') ? (
            <div className="media-slide" key={item.id}>
              <video
                src={item.url}
                controls
                playsInline
                preload="metadata"
                onClick={(event) => event.stopPropagation()}
                onLoadedMetadata={(event) => {
                  const video = event.currentTarget
                  if (video.videoWidth && video.videoHeight) {
                    recordRatio(item.id, video.videoWidth / video.videoHeight)
                  }
                }}
              />
              <span className="video-badge media-overlay-badge">
                <span className="material-symbols-outlined">videocam</span>
              </span>
            </div>
          ) : (
            <div className="media-slide" key={item.id}>
              <img
                src={item.url}
                alt=""
                onLoad={(event) => {
                  const img = event.currentTarget
                  if (img.naturalWidth && img.naturalHeight) {
                    recordRatio(item.id, img.naturalWidth / img.naturalHeight)
                  }
                }}
              />
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
          <span className="carousel-counter media-overlay-badge">
            {index + 1}/{media.length}
          </span>
        </>
      )}
    </div>
  )
}
