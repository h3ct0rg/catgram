import { FormEvent, useEffect, useState } from 'react'
import { getMyShelter, updateMyShelter } from '../../services/apiClient'
import { Shelter } from '../../types/domain'
import { LocationPickerMap } from '../../components/map/LocationPickerMap'

export function MyShelterPage() {
  const [shelter, setShelter] = useState<Shelter | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    getMyShelter()
      .then(setShelter)
      .catch(() => setError('No se pudo cargar tu refugio.'))
      .finally(() => setLoading(false))
  }, [])

  function update<K extends keyof Shelter>(key: K, value: Shelter[K]) {
    setShelter((current) => (current ? { ...current, [key]: value } : current))
    setSaved(false)
  }

  function handleLocationChange(lat: number, lng: number) {
    setShelter((current) => (current ? { ...current, latitude: lat, longitude: lng } : current))
    setSaved(false)
  }

  async function submit(event: FormEvent) {
    event.preventDefault()
    if (!shelter) return
    setSaving(true)
    setError('')
    try {
      const updated = await updateMyShelter({
        name: shelter.name,
        description: shelter.description,
        address: shelter.address,
        city: shelter.city,
        country: shelter.country,
        phone: shelter.phone ?? undefined,
        whatsApp: shelter.whatsApp ?? undefined,
        email: shelter.email ?? undefined,
        latitude: shelter.latitude ?? undefined,
        longitude: shelter.longitude ?? undefined,
      })
      setShelter(updated)
      setSaved(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo guardar el refugio.')
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <p className="body-copy">Cargando…</p>
  if (!shelter)
    return (
      <p className="feedback" role="status">
        {error || 'Refugio no encontrado.'}
      </p>
    )

  return (
    <div>
      <div className="admin-header">
        <div className="admin-header-title">
          <span className="admin-header-icon">🏠</span>
          <div>
            <p className="eyebrow">Panel admin</p>
            <h1>Mi refugio</h1>
          </div>
        </div>
      </div>

      <form className="register-form" onSubmit={submit}>
        <label>
          Nombre
          <input
            value={shelter.name}
            onChange={(event) => update('name', event.target.value)}
            required
          />
        </label>
        <label>
          Descripción
          <textarea
            rows={3}
            value={shelter.description}
            onChange={(event) => update('description', event.target.value)}
          />
        </label>
        <label>
          Dirección
          <input
            value={shelter.address}
            onChange={(event) => update('address', event.target.value)}
          />
        </label>
        <div className="two-columns">
          <label>
            Ciudad
            <input value={shelter.city} onChange={(event) => update('city', event.target.value)} />
          </label>
          <label>
            País
            <input
              value={shelter.country}
              onChange={(event) => update('country', event.target.value)}
            />
          </label>
        </div>
        <div className="two-columns">
          <label>
            Teléfono
            <input
              value={shelter.phone ?? ''}
              onChange={(event) => update('phone', event.target.value)}
            />
          </label>
          <label>
            WhatsApp
            <input
              value={shelter.whatsApp ?? ''}
              onChange={(event) => update('whatsApp', event.target.value)}
            />
          </label>
        </div>
        <label>
          Email de contacto
          <input
            value={shelter.email ?? ''}
            onChange={(event) => update('email', event.target.value)}
          />
        </label>

        {/* Google Maps Pin Picker */}
        <LocationPickerMap
          latitude={shelter.latitude}
          longitude={shelter.longitude}
          onChange={handleLocationChange}
          shelterName={shelter.name}
        />

        {error && (
          <p className="feedback" role="status">
            {error}
          </p>
        )}
        {saved && <p className="body-copy">Guardado con éxito. 🐾</p>}
        <button className="primary-button" type="submit" disabled={saving}>
          {saving ? 'Guardando…' : 'Guardar refugio'}
        </button>
      </form>
    </div>
  )
}

