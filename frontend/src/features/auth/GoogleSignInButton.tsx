import { useEffect, useRef } from 'react'


let googleScriptPromise: Promise<void> | null = null

function loadGoogleScript(): Promise<void> {
  if (window.google?.accounts?.id) return Promise.resolve()
  if (!googleScriptPromise) {
    googleScriptPromise = new Promise((resolve, reject) => {
      const script = document.createElement('script')
      script.src = 'https://accounts.google.com/gsi/client'
      script.async = true
      script.onload = () => resolve()
      script.onerror = () => reject(new Error('No se pudo cargar Google Sign-In.'))
      document.head.appendChild(script)
    })
  }
  return googleScriptPromise
}

type Props = {
  onCredential: (idToken: string) => void
}

export function GoogleSignInButton({ onCredential }: Props) {
  const containerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID
    if (!clientId) return
    let cancelled = false

    loadGoogleScript().then(() => {
      if (cancelled || !containerRef.current || !window.google?.accounts?.id) return
      window.google.accounts.id.initialize({
        client_id: clientId,
        callback: (response: { credential: string }) => onCredential(response.credential),
      })
      window.google.accounts.id.renderButton(containerRef.current, {
        theme: 'outline',
        size: 'large',
        shape: 'pill',
        width: 280,
      })
    })

    return () => {
      cancelled = true
    }
  }, [onCredential])

  return <div ref={containerRef} style={{ display: 'flex', justifyContent: 'center' }} />
}
