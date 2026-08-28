import { useEffect, useState } from 'react'

let googleMapsPromise: Promise<typeof google.maps> | null = null

export function useGoogleMaps() {
  const [loaded, setLoaded] = useState(
    typeof window !== 'undefined' && typeof window.google?.maps !== 'undefined',
  )
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (typeof window === 'undefined') return

    if (window.google?.maps) {
      setLoaded(true)
      return
    }

    const apiKey = import.meta.env.VITE_GOOGLE_MAPS_API_KEY || ''
    if (!apiKey) {
      setError('VITE_GOOGLE_MAPS_API_KEY no está configurada.')
      return
    }

    if (!googleMapsPromise) {
      googleMapsPromise = new Promise((resolve, reject) => {
        const existingScript = document.getElementById('google-maps-script')
        if (existingScript) {
          existingScript.addEventListener('load', () => resolve(window.google.maps))
          existingScript.addEventListener('error', () =>
            reject(new Error('Error al cargar Google Maps.')),
          )
          return
        }

        const script = document.createElement('script')
        script.id = 'google-maps-script'
        script.src = `https://maps.googleapis.com/maps/api/js?key=${apiKey}&libraries=places`
        script.async = true
        script.defer = true
        script.onload = () => resolve(window.google.maps)
        script.onerror = () => reject(new Error('No se pudo cargar el script de Google Maps.'))
        document.head.appendChild(script)
      })
    }

    googleMapsPromise
      .then(() => {
        setLoaded(true)
      })
      .catch((err) => {
        setError(err instanceof Error ? err.message : 'Error desconocido al cargar Google Maps.')
      })
  }, [])

  return { loaded, error }
}
