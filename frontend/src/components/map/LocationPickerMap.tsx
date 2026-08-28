import { useEffect, useRef, useState } from 'react'
import { useGoogleMaps } from '../../utils/useGoogleMaps'

type Props = {
  latitude: number | null
  longitude: number | null
  onChange: (lat: number, lng: number) => void
  shelterName?: string
}

const DEFAULT_CENTER = { lat: 4.6097, lng: -74.0817 } // Bogotá as general fallback

export function LocationPickerMap({ latitude, longitude, onChange, shelterName }: Props) {
  const { loaded, error } = useGoogleMaps()
  const mapRef = useRef<HTMLDivElement>(null)
  const googleMapRef = useRef<google.maps.Map | null>(null)
  const markerRef = useRef<google.maps.Marker | null>(null)
  const [geoLocating, setGeoLocating] = useState(false)
  const [geoError, setGeoError] = useState('')

  // Initialize Map
  useEffect(() => {
    if (!loaded || !mapRef.current) return

    // If map is already initialized, just update marker/center
    if (!googleMapRef.current) {
      const initialCenter =
        latitude && longitude
          ? { lat: latitude, lng: longitude }
          : DEFAULT_CENTER

      const initialZoom = latitude && longitude ? 15 : 12

      const map = new window.google.maps.Map(mapRef.current, {
        center: initialCenter,
        zoom: initialZoom,
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
        position: initialCenter,
        map: latitude && longitude ? map : null,
        draggable: true,
        title: shelterName || 'Ubicación del Refugio',
        animation: window.google.maps.Animation.DROP,
      })

      marker.addListener('dragend', () => {
        const position = marker.getPosition()
        if (position) {
          onChange(Number(position.lat().toFixed(6)), Number(position.lng().toFixed(6)))
        }
      })

      map.addListener('click', (event: google.maps.MapMouseEvent) => {
        if (event.latLng) {
          const lat = Number(event.latLng.lat().toFixed(6))
          const lng = Number(event.latLng.lng().toFixed(6))
          marker.setPosition(event.latLng)
          marker.setMap(map)
          onChange(lat, lng)
        }
      })

      googleMapRef.current = map
      markerRef.current = marker

      // If no initial coordinates were provided, try to center on current user location automatically
      if (!latitude || !longitude) {
        locateCurrentPosition(map, marker)
      }
    }
  }, [loaded])

  // Sync position if latitude/longitude props change from outside
  useEffect(() => {
    if (googleMapRef.current && markerRef.current && latitude && longitude) {
      const currentPos = markerRef.current.getPosition()
      const isDifferent =
        !currentPos ||
        Math.abs(currentPos.lat() - latitude) > 0.0001 ||
        Math.abs(currentPos.lng() - longitude) > 0.0001

      if (isDifferent) {
        const newPos = { lat: latitude, lng: longitude }
        markerRef.current.setPosition(newPos)
        markerRef.current.setMap(googleMapRef.current)
        googleMapRef.current.panTo(newPos)
      }
    }
  }, [latitude, longitude])

  function locateCurrentPosition(
    mapInstance = googleMapRef.current,
    markerInstance = markerRef.current,
  ) {
    if (!navigator.geolocation) {
      setGeoError('Tu navegador no soporta geolocalización.')
      return
    }

    setGeoLocating(true)
    setGeoError('')

    navigator.geolocation.getCurrentPosition(
      (position) => {
        setGeoLocating(false)
        const lat = Number(position.coords.latitude.toFixed(6))
        const lng = Number(position.coords.longitude.toFixed(6))
        const userPos = { lat, lng }

        if (mapInstance) {
          mapInstance.setCenter(userPos)
          mapInstance.setZoom(16)
        }

        if (markerInstance && mapInstance) {
          markerInstance.setPosition(userPos)
          markerInstance.setMap(mapInstance)
        }

        onChange(lat, lng)
      },
      (err) => {
        setGeoLocating(false)
        console.warn('Geolocation error:', err)
        setGeoError('No pudimos acceder a tu ubicación actual. Puedes hacer clic en el mapa para marcarla.')
      },
      { enableHighAccuracy: true, timeout: 10000 },
    )
  }

  return (
    <div className="location-picker-wrap">
      <div className="location-picker-header">
        <span className="location-picker-label">
          <span className="material-symbols-outlined">pin_drop</span>
          Pin en Google Maps (Haz clic o arrastra el marcador)
        </span>
        <button
          type="button"
          className="locate-me-btn"
          onClick={() => locateCurrentPosition()}
          disabled={geoLocating}
        >
          <span className="material-symbols-outlined">my_location</span>
          {geoLocating ? 'Ubicando…' : 'Mi ubicación actual'}
        </button>
      </div>

      {error ? (
        <div className="location-map-error">
          <p>{error}</p>
        </div>
      ) : (
        <div className="location-map-container" ref={mapRef}>
          {!loaded && (
            <div className="location-map-loading">
              <span className="material-symbols-outlined rotating">progress_activity</span>
              <span>Cargando Google Maps…</span>
            </div>
          )}
        </div>
      )}

      {geoError && <p className="location-picker-note error">{geoError}</p>}
      {latitude && longitude ? (
        <p className="location-picker-coords">
          📍 Lat: <strong>{latitude}</strong>, Lng: <strong>{longitude}</strong>
        </p>
      ) : (
        <p className="location-picker-note">
          💡 Haz clic en el mapa o pulsa "Mi ubicación actual" para fijar la posición de tu refugio.
        </p>
      )}
    </div>
  )
}
