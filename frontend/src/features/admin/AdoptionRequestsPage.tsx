import { useEffect, useState } from 'react'
import { getAdoptionRequests, updateAdoptionRequestStatus } from '../../services/apiClient'
import { AdoptionRequest, AdoptionRequestStatus } from '../../types/adoption'

const STATUS_OPTIONS: AdoptionRequestStatus[] = [
  'Pending',
  'InReview',
  'Approved',
  'Rejected',
  'Completed',
]

const NEXT_STATUSES: Record<AdoptionRequestStatus, AdoptionRequestStatus[]> = {
  Pending: ['InReview', 'Rejected'],
  InReview: ['Approved', 'Rejected'],
  Approved: ['Completed', 'Rejected'],
  Rejected: [],
  Completed: [],
}

export function AdoptionRequestsPage() {
  const [requests, setRequests] = useState<AdoptionRequest[]>([])
  const [statusFilter, setStatusFilter] = useState<AdoptionRequestStatus | ''>('Pending')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [notes, setNotes] = useState<Record<string, string>>({})

  useEffect(() => {
    setLoading(true)
    getAdoptionRequests({ status: statusFilter || undefined })
      .then(setRequests)
      .catch(() => setError('No se pudieron cargar las solicitudes.'))
      .finally(() => setLoading(false))
  }, [statusFilter])

  async function transition(request: AdoptionRequest, status: AdoptionRequestStatus) {
    try {
      const updated = await updateAdoptionRequestStatus(request.id, status, notes[request.id])
      setRequests((current) => current.map((item) => (item.id === request.id ? updated : item)))
    } catch {
      setError('No se pudo actualizar la solicitud.')
    }
  }

  return (
    <div>
      <div className="admin-header">
        <div className="admin-header-title">
          <span className="admin-header-icon">🐕</span>
          <div>
            <p className="eyebrow">Panel admin</p>
            <h1>Solicitudes de adopción</h1>
          </div>
        </div>
      </div>

      <div className="admin-toolbar">
        <select
          value={statusFilter}
          onChange={(event) => setStatusFilter(event.target.value as AdoptionRequestStatus | '')}
        >
          <option value="">Todos los estados</option>
          {STATUS_OPTIONS.map((status) => (
            <option key={status} value={status}>
              {status}
            </option>
          ))}
        </select>
      </div>
      {loading && <p className="body-copy">Cargando…</p>}
      {error && (
        <p className="feedback" role="status">
          {error}
        </p>
      )}
      <div className="admin-table">
        {requests.map((request) => (
          <div className="admin-row" key={request.id}>
            <div>
              <strong>{request.animalName}</strong> · {request.applicantUserName}
              <p>
                {Object.entries(request.answers)
                  .map(([key, value]) => `${key}: ${value}`)
                  .join(' · ')}
              </p>
              <small>{new Date(request.createdAt).toLocaleString('es-ES')}</small>
              <div>
                <input
                  placeholder="Notas de revisión (opcional)"
                  value={notes[request.id] ?? request.reviewNotes ?? ''}
                  onChange={(event) =>
                    setNotes((current) => ({ ...current, [request.id]: event.target.value }))
                  }
                />
              </div>
            </div>
            <div className="admin-row-actions">
              <span className={`badge badge-${request.status.toLowerCase()}`}>
                {request.status}
              </span>
              {NEXT_STATUSES[request.status].map((status) => (
                <button
                  key={status}
                  className="secondary-button"
                  onClick={() => transition(request, status)}
                >
                  {status}
                </button>
              ))}
            </div>
          </div>
        ))}
        {!loading && requests.length === 0 && (
          <p className="admin-empty">No hay solicitudes con este filtro.</p>
        )}
      </div>
    </div>
  )
}
