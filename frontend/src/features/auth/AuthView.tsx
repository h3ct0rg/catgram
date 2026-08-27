import { FormEvent, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useSession } from '../../context/SessionContext'
import { googleChallenge, login } from '../../services/apiClient'

type Props = { mode: 'login' | 'invite' }

export function AuthView({ mode }: Props) {
  const [message, setMessage] = useState('')
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const session = useSession()

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setLoading(true)
    setMessage('')
    const form = new FormData(event.currentTarget)
    try {
      const result = await login(String(form.get('userName')), String(form.get('password')))
      session.login(result.accessToken)
      navigate('/')
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Ocurrió un error.')
    } finally {
      setLoading(false)
    }
  }

  const invitationToken = searchParams.get('invitationToken') ?? undefined

  return (
    <main className="auth-shell">
      <button className="back-button" onClick={() => navigate('/')}>
        ‹ Volver
      </button>
      <section className="glass-card auth-card">
        <div className="brand-logo">🐾</div>
        <p className="eyebrow">Kindred Paws</p>
        <h1>{mode === 'invite' ? 'Acepta tu invitación' : 'Bienvenido de nuevo'}</h1>
        <p className="body-copy">
          {mode === 'invite'
            ? 'Inicia con Google para unirte a la comunidad autorizada.'
            : 'Los refugios construyen historias. Las familias encuentran un hogar.'}
        </p>
        {mode === 'login' ? (
          <form onSubmit={submit} className="auth-form">
            <label>
              Usuario
              <input name="userName" autoComplete="username" required />
            </label>
            <label>
              Contraseña
              <input name="password" type="password" autoComplete="current-password" required />
            </label>
            <button className="primary-button" disabled={loading}>
              {loading ? 'Ingresando…' : 'Iniciar sesión'}
            </button>
          </form>
        ) : (
          <button className="primary-button" onClick={() => googleChallenge(invitationToken)}>
            Continuar con Google
          </button>
        )}
        {message && (
          <p className="feedback" role="status">
            {message}
          </p>
        )}
      </section>
    </main>
  )
}
