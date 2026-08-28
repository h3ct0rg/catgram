import { useEffect, useRef } from 'react'
import { useGoogleMaps } from '../../utils/useGoogleMaps'

type Props = {
  latitude: number | null
  longitude: number | null
  shelterName: string
  address?: string
  city?: string
}

export function ShelterLocationMap({
  latitude,
  longitude,
  shelterName,
  address,
  city,
}: Props) {
  const { loaded, error } = useGoogleMaps()
  const mapRef = useRef<HTMLDivElement>(null)
  const googleMapRef = useRef<google.maps.Map | null>(null)

  const hasCoords = typeof latitude === 'number' && typeof longitude === 'number'

  useEffect(() => {
    if (!loaded || !mapRef.current || !hasCoords) return

    const position = { lat: latitude!, lng: longitude! }

    if (!googleMapRef.current) {
      const map = new window.google.maps.Map(mapRef.current, {
        center: position,
        zoom: 15,
        mapTypeControl: false,
        streetViewControl: false,
        fullscreenControl: true,
        zoomControl: true,
        styles: [
          {
            featureType: 'poi',
            elementType: 'labels',
            stylers: [{ visibility: 'off' }],
          },
        ],
      })

      const marker = new window.google.maps.Marker({
        position,
        map,
        title: shelterName,
        animation: window.google.maps.Animation.DROP,
      })

      const infoContent = `
        <div style="color: #181c23; font-family: 'Plus Jakarta Sans', sans-serif; padding: 4px;">
          <strong style="font-size: 14px; display: block; margin-bottom: 2px;">${shelterName}</strong>
          ${address ? `<span style="font-size: 12px; color: #5d6371;">${address}${city ? `, ${city}` : ''}</span>` : ''}
        </div>
      `
      const infoWindow = new window.google.maps.InfoWindow({
        content: infoContent,
      })

      marker.addListener('click', () => {
        infoWindow.open(map, marker)
      })

      googleMapRef.current = map
    } else {
      googleMapRef.current.panTo(position)
    }
  }, [loaded, latitude, longitude, hasCoords, shelterName, address, city])

  if (!hasCoords) {
    return (
      <div className="location-map-placeholder">
        <span className="material-symbols-outlined">location_off</span>
        <span>El refugio aún no ha fijado su pin en el mapa.</span>
      </div>
    )
  }

  const googleMapsDirectionsUrl = `https://www.google.com/maps/dir/?api=1&destination=${latitude},${longitude}`

  return (
    <div className="shelter-map-display-wrap">
      {error ? (
        <div className="location-map-error">
          <p>{error}</p>
        </div>
      ) : (
        <div className="shelter-map-container" ref={mapRef}>
          {!loaded && (
            <div className="location-map-loading">
              <span className="material-symbols-outlined rotating">progress_activity</span>
              <span>Cargando mapa…</span>
            </div>
          )}
        </div>
      )}

      <div className="shelter-map-footer">
        <div className="shelter-map-info">
          <strong>{shelterName}</strong>
          {address && <span>📍 {address}{city ? `, ${city}` : ''}</span>}
        </div>
        <a
          href={googleMapsDirectionsUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="map-directions-btn"
        >
          <span className="material-symbols-outlined">directions</span>
          Cómo llegar
        </a>
      </div>
    </div>
  )
}
