import { FormEvent, useState } from 'react'
import { createAdoptionRequest } from '../../services/apiClient'

type Props = { animalId: string; animalName: string; onClose: () => void }

const QUESTIONS: Array<{ key: string; label: string }> = [
  { key: 'tipoVivienda', label: '¿Qué tipo de vivienda tienes?' },
  { key: 'tienePatio', label: '¿Tienes patio o espacio exterior?' },
  { key: 'tieneOtrosAnimales', label: '¿Tienes otras mascotas en casa?' },
  { key: 'tieneNinos', label: '¿Hay niños en casa?' },
  { key: 'experiencia', label: '¿Qué experiencia tienes con mascotas?' },
]

export function AdoptionRequestModal({ animalId, animalName, onClose }: Props) {
  const [answers, setAnswers] = useState<Record<string, string>>({})
  const [submitting, setSubmitting] = useState(false)
  const [done, setDone] = useState(false)
  const [error, setError] = useState('')

  async function submit(event: FormEvent) {
    event.preventDefault()
    setSubmitting(true)
    setError('')
    try {
      await createAdoptionRequest(animalId, answers)
      setDone(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo enviar la solicitud.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="sheet-overlay" role="dialog" aria-modal="true" onClick={onClose}>
      <div className="sheet" onClick={(event) => event.stopPropagation()}>
        <div className="sheet-handle" />
        <h2>Solicitar adopción de {animalName}</h2>
        {done ? (
          <>
            <p className="body-copy">
              ¡Listo! El refugio revisará tu solicitud y te avisaremos por notificación.
            </p>
            <button className="primary-button" onClick={onClose}>
              Cerrar
            </button>
          </>
        ) : (
          <form className="auth-form" onSubmit={submit}>
            {QUESTIONS.map((question) => (
              <label key={question.key}>
                {question.label}
                <input
                  value={answers[question.key] ?? ''}
                  onChange={(event) =>
                    setAnswers((current) => ({ ...current, [question.key]: event.target.value }))
                  }
                  required
                />
              </label>
            ))}
            {error && (
              <p className="feedback" role="status">
                {error}
              </p>
            )}
            <button className="primary-button" disabled={submitting}>
              {submitting ? 'Enviando…' : 'Enviar solicitud'}
            </button>
          </form>
        )}
      </div>
    </div>
  )
}
