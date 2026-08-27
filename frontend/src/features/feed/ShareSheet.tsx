import { useState } from 'react'

type Props = {
  url: string
  title: string
  text: string
  onClose: () => void
}

export function ShareSheet({ url, title, text, onClose }: Props) {
  const [copied, setCopied] = useState(false)
  const canUseWebShare = typeof navigator.share === 'function'

  async function share() {
    try {
      await navigator.share({ title, text, url })
      onClose()
    } catch {
      // user cancelled the native share sheet — leave the fallback sheet open
    }
  }

  async function copyLink() {
    try {
      await navigator.clipboard.writeText(url)
      setCopied(true)
    } catch {
      setCopied(false)
    }
  }

  const whatsapp = `https://wa.me/?text=${encodeURIComponent(`${text} ${url}`)}`
  const facebook = `https://www.facebook.com/sharer/sharer.php?u=${encodeURIComponent(url)}`
  const x = `https://twitter.com/intent/tweet?url=${encodeURIComponent(url)}&text=${encodeURIComponent(text)}`

  return (
    <div className="sheet-overlay" role="dialog" aria-modal="true" onClick={onClose}>
      <div className="sheet" onClick={(event) => event.stopPropagation()}>
        <div className="sheet-handle" />
        <h2>Compartir</h2>
        {canUseWebShare && (
          <button className="primary-button" onClick={share}>
            Compartir…
          </button>
        )}
        <div className="share-links">
          <a className="share-link" href={whatsapp} target="_blank" rel="noreferrer">
            🟢 WhatsApp
          </a>
          <a className="share-link" href={facebook} target="_blank" rel="noreferrer">
            🔵 Facebook
          </a>
          <a className="share-link" href={x} target="_blank" rel="noreferrer">
            ⚫ X
          </a>
          <button className="share-link" onClick={copyLink}>
            🔗 {copied ? 'Enlace copiado' : 'Copiar enlace'}
          </button>
        </div>
        <button className="secondary-button" onClick={onClose}>
          Cerrar
        </button>
      </div>
    </div>
  )
}
