import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { BottomNav } from '../../components/layout/BottomNav'
import { TopBar } from '../../components/layout/TopBar'
import { getAnimals, getNearbyAnimals } from '../../services/apiClient'
import { Animal } from '../../types/domain'
import { adoptionStatusLabel } from '../../utils/adoptionStatus'

export function SearchPage() {
  const navigate = useNavigate()
  const [name, setName] = useState('')
  const [species, setSpecies] = useState('')
  const [sex, setSex] = useState('')
  const [size, setSize] = useState('')
  const [breed, setBreed] = useState('')
  const [location, setLocation] = useState('')
  const [adoptionStatus, setAdoptionStatus] = useState('Available')
  const [results, setResults] = useState<Animal[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [searched, setSearched] = useState(false)

  async function search() {
    setLoading(true)
    setError('')
    setSearched(true)
    try {
      const items = await getAnimals({
        name: name || undefined,
        species: species || undefined,
        sex: sex || undefined,
        size: size || undefined,
        breed: breed || undefined,
        location: location || undefined,
        adoptionStatus: adoptionStatus || undefined,
      })
      setResults(items)
    } catch {
      setError('No se pudo buscar. Intenta de nuevo.')
    } finally {
      setLoading(false)
    }
  }

  function searchNearby() {
    if (!navigator.geolocation) {
      setError('Tu navegador no soporta geolocalización.')
      return
    }
    setLoading(true)
    setError('')
    setSearched(true)
    navigator.geolocation.getCurrentPosition(
      async (position) => {
        try {
          const items = await getNearbyAnimals(position.coords.latitude, position.coords.longitude)
          setResults(items)
        } catch {
          setError('No se pudo buscar cerca de ti.')
        } finally {
          setLoading(false)
        }
      },
      () => {
        setError('No pudimos acceder a tu ubicación. Revisa los permisos del navegador.')
        setLoading(false)
      },
    )
  }

  return (
    <div className="app-shell">
      <TopBar onHome={() => navigate('/')} />
      <main className="feed-page">
        <div className="feed-heading">
          <div>
            <p className="eyebrow">Encuentra un compañero</p>
            <h1>Buscar animales</h1>
          </div>
        </div>
        <div className="search-filters glass-card">
          <input
            placeholder="Nombre"
            value={name}
            onChange={(event) => setName(event.target.value)}
          />
          <div className="two-columns">
            <select value={species} onChange={(event) => setSpecies(event.target.value)}>
              <option value="">Especie</option>
              <option value="Dog">Perro</option>
              <option value="Cat">Gato</option>
              <option value="Other">Otro</option>
            </select>
            <select value={sex} onChange={(event) => setSex(event.target.value)}>
              <option value="">Sexo</option>
              <option value="Female">Hembra</option>
              <option value="Male">Macho</option>
              <option value="Unknown">Desconocido</option>
            </select>
          </div>
          <div className="two-columns">
            <select value={size} onChange={(event) => setSize(event.target.value)}>
              <option value="">Tamaño</option>
              <option value="Small">Pequeño</option>
              <option value="Medium">Mediano</option>
              <option value="Large">Grande</option>
            </select>
            <select
              value={adoptionStatus}
              onChange={(event) => setAdoptionStatus(event.target.value)}
            >
              <option value="">Cualquier estado</option>
              <option value="Available">Disponible</option>
              <option value="InProcess">En proceso</option>
              <option value="Adopted">Adoptado</option>
            </select>
          </div>
          <input
            placeholder="Raza"
            value={breed}
            onChange={(event) => setBreed(event.target.value)}
          />
          <input
            placeholder="Ubicación"
            value={location}
            onChange={(event) => setLocation(event.target.value)}
          />
          <div className="search-actions">
            <button className="primary-button" onClick={search} disabled={loading}>
              Buscar
            </button>
            <button className="secondary-button" onClick={searchNearby} disabled={loading}>
              📍 Cerca de mí
            </button>
          </div>
        </div>

        {loading && <p className="body-copy">Buscando…</p>}
        {error && (
          <p className="feedback" role="status">
            {error}
          </p>
        )}
        {!loading && searched && results.length === 0 && !error && (
          <p className="body-copy">No encontramos animales con estos filtros.</p>
        )}

        <div className="search-results">
          {results.map((animal) => {
            const primary = animal.media.find((media) => media.isPrimary) ?? animal.media[0]
            return (
              <button
                className="search-result-card"
                key={animal.id}
                onClick={() => navigate(`/animals/${animal.id}`)}
              >
                {primary && <img src={primary.thumbnailUrl ?? primary.url} alt={animal.name} />}
                <div>
                  <strong>{animal.name}</strong>
                  <small>
                    {animal.shelterName} · {adoptionStatusLabel(animal.adoptionStatus)}
                  </small>
                </div>
              </button>
            )
          })}
        </div>
      </main>
      <BottomNav
        onHome={() => navigate('/')}
        onSearch={() => undefined}
        onCreate={() => navigate('/animals/new')}
      />
    </div>
  )
}
