import { useEffect, useState } from 'react'
import { getAuditLogs } from '../../services/apiClient'
import { AuditLogEntry } from '../../types/admin'

export function AuditLogPage() {
  const [logs, setLogs] = useState<AuditLogEntry[]>([])
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    getAuditLogs({})
      .then(setLogs)
      .catch(() => setError('No se pudo cargar la auditoría.'))
      .finally(() => setLoading(false))
  }, [])

  return (
    <div>
      <div className="admin-header">
        <div className="admin-header-title">
          <span className="admin-header-icon">🧾</span>
          <div>
            <p className="eyebrow">Panel admin</p>
            <h1>Auditoría</h1>
          </div>
        </div>
      </div>

      {loading && <p className="body-copy">Cargando…</p>}
      {error && (
        <p className="feedback" role="status">
          {error}
        </p>
      )}
      <div className="admin-table">
        {logs.map((log) => (
          <div className="admin-row" key={log.id}>
            <div>
              <strong>{log.action}</strong> sobre {log.entityType} ({log.entityId})
              {log.details && <p>{log.details}</p>}
              <small>
                {log.actorUserName} · {new Date(log.createdAt).toLocaleString('es-ES')}
              </small>
            </div>
          </div>
        ))}
        {!loading && logs.length === 0 && <p className="admin-empty">Sin actividad registrada.</p>}
      </div>
    </div>
  )
}
