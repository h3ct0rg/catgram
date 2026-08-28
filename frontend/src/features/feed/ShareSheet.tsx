import { useState } from 'react'
import { registerPostShare } from '../../services/apiClient'

type Props = {
  url: string
  title: string
  text: string
  postId?: string
  onClose: () => void
}

const DESTINATIONS = [
  { key: 'whatsapp', label: 'WhatsApp', icon: '💬', className: 'share-tile-whatsapp' },
  { key: 'facebook', label: 'Facebook', icon: '📘', className: 'share-tile-facebook' },
  { key: 'x', label: 'X', icon: '✕', className: 'share-tile-x' },
] as const

export function ShareSheet({ url, title, text, postId, onClose }: Props) {
  const [copied, setCopied] = useState(false)
  const canUseWebShare = typeof navigator.share === 'function'

  function trackShare() {
    if (postId) registerPostShare(postId).catch(() => undefined)
  }

  async function share() {
    try {
      await navigator.share({ title, text, url })
      trackShare()
      onClose()
    } catch {
      // user cancelled the native share sheet — leave the fallback sheet open
    }
  }

  async function copyLink() {
    try {
      await navigator.clipboard.writeText(url)
      setCopied(true)
      trackShare()
    } catch {
      setCopied(false)
    }
  }

  const links: Record<(typeof DESTINATIONS)[number]['key'], string> = {
    whatsapp: `https://wa.me/?text=${encodeURIComponent(`${text} ${url}`)}`,
    facebook: `https://www.facebook.com/sharer/sharer.php?u=${encodeURIComponent(url)}`,
    x: `https://twitter.com/intent/tweet?url=${encodeURIComponent(url)}&text=${encodeURIComponent(text)}`,
  }

  return (
    <div className="sheet-overlay" role="dialog" aria-modal="true" onClick={onClose}>
      <div className="sheet share-sheet" onClick={(event) => event.stopPropagation()}>
        <div className="sheet-handle" />
        <h2>Compartir</h2>

        <div className="share-grid">
          {DESTINATIONS.map((destination) => (
            <a
              key={destination.key}
              className="share-tile"
              href={links[destination.key]}
              target="_blank"
              rel="noreferrer"
              onClick={trackShare}
            >
              <span className={`share-tile-icon ${destination.className}`}>{destination.icon}</span>
              <span>{destination.label}</span>
            </a>
          ))}
          <button className="share-tile" onClick={copyLink}>
            <span className={`share-tile-icon share-tile-copy ${copied ? 'copied' : ''}`}>
              <span className="material-symbols-outlined">{copied ? 'check' : 'link'}</span>
            </span>
            <span>{copied ? 'Enlace copiado' : 'Copiar enlace'}</span>
          </button>
        </div>

        {canUseWebShare && (
          <button className="share-native" onClick={share}>
            <span className="material-symbols-outlined">ios_share</span>
            Más opciones…
          </button>
        )}

        <button className="secondary-button" onClick={onClose}>
          Cerrar
        </button>
      </div>
    </div>
  )
}
