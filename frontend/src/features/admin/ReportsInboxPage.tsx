import { useEffect, useState } from 'react'
import { getReports, resolveReport } from '../../services/apiClient'
import { AdminReport, ReportStatus } from '../../types/admin'

const STATUS_OPTIONS: ReportStatus[] = ['Pending', 'Reviewed', 'Resolved', 'Dismissed']

export function ReportsInboxPage() {
  const [reports, setReports] = useState<AdminReport[]>([])
  const [statusFilter, setStatusFilter] = useState<ReportStatus | ''>('Pending')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    setLoading(true)
    getReports({ status: statusFilter || undefined })
      .then(setReports)
      .catch(() => setError('No se pudieron cargar los reportes.'))
      .finally(() => setLoading(false))
  }, [statusFilter])

  async function resolve(id: string, status: ReportStatus) {
    try {
      const updated = await resolveReport(id, status)
      setReports((current) => current.map((r) => (r.id === id ? updated : r)))
    } catch {
      setError('No se pudo actualizar el reporte.')
    }
  }

  return (
    <div>
      <div className="admin-header">
        <div className="admin-header-title">
          <span className="admin-header-icon">🚩</span>
          <div>
            <p className="eyebrow">Panel admin</p>
            <h1>Reportes</h1>
          </div>
        </div>
      </div>

      <div className="admin-toolbar">
        <select
          value={statusFilter}
          onChange={(event) => setStatusFilter(event.target.value as ReportStatus | '')}
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
        {reports.map((report) => (
          <div className="admin-row" key={report.id}>
            <div>
              <strong>{report.targetType}</strong> · {report.targetId}
              <p>{report.reason}</p>
              <small>{new Date(report.createdAt).toLocaleString('es-ES')}</small>
            </div>
            <div className="admin-row-actions">
              <span className={`badge badge-${report.status.toLowerCase()}`}>{report.status}</span>
              {STATUS_OPTIONS.filter((status) => status !== report.status).map((status) => (
                <button
                  key={status}
                  className="secondary-button"
                  onClick={() => resolve(report.id, status)}
                >
                  {status}
                </button>
              ))}
            </div>
          </div>
        ))}
        {!loading && reports.length === 0 && (
          <p className="admin-empty">No hay reportes con este filtro.</p>
        )}
      </div>
    </div>
  )
}
