import { FormEvent, useState } from 'react'
import { createReport } from '../../services/apiClient'
import { REPORT_REASONS, ReportTargetType } from '../../types/social'

type Props = {
  targetType: ReportTargetType
  targetId: string
  onClose: () => void
}

export function ReportModal({ targetType, targetId, onClose }: Props) {
  const [reason, setReason] = useState<string>(REPORT_REASONS[0])
  const [details, setDetails] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [done, setDone] = useState(false)
  const [error, setError] = useState('')

  async function submit(event: FormEvent) {
    event.preventDefault()
    setSubmitting(true)
    setError('')
    try {
      await createReport(targetType, targetId, details ? `${reason}: ${details}` : reason)
      setDone(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo enviar el reporte.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="sheet-overlay" role="dialog" aria-modal="true" onClick={onClose}>
      <div className="sheet" onClick={(event) => event.stopPropagation()}>
        <div className="sheet-handle" />
        <h2>Reportar</h2>
        {done ? (
          <>
            <p className="body-copy">Gracias, revisaremos tu reporte.</p>
            <button className="primary-button" onClick={onClose}>
              Cerrar
            </button>
          </>
        ) : (
          <form className="auth-form" onSubmit={submit}>
            <label>
              Motivo
              <select value={reason} onChange={(event) => setReason(event.target.value)}>
                {REPORT_REASONS.map((option) => (
                  <option key={option} value={option}>
                    {option}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Detalles (opcional)
              <textarea
                rows={3}
                value={details}
                onChange={(event) => setDetails(event.target.value)}
              />
            </label>
            {error && (
              <p className="feedback" role="status">
                {error}
              </p>
            )}
            <button className="primary-button" disabled={submitting}>
              {submitting ? 'Enviando…' : 'Enviar reporte'}
            </button>
          </form>
        )}
      </div>
    </div>
  )
}
