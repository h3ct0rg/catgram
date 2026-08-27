import { FormEvent, useState } from 'react'
import { apiBaseUrl, googleChallenge, login } from '../../services/apiClient'

type Props = { mode: 'login' | 'invite'; onBack: () => void }

export function AuthView({ mode, onBack }: Props) {
  const [message, setMessage] = useState('')
  const [loading, setLoading] = useState(false)

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setLoading(true); setMessage('')
    const form = new FormData(event.currentTarget)
    try { const result = await login(String(form.get('userName')), String(form.get('password'))); sessionStorage.setItem('kindred_paws_access_token', result.accessToken); window.location.hash = 'feed' }
    catch (error) { setMessage(error instanceof Error ? error.message : 'Ocurrió un error.') }
    finally { setLoading(false) }
  }

  const invitationToken = new URLSearchParams(window.location.search).get('invitationToken') ?? undefined
  return (
    <main className="auth-shell">
      <button className="back-button" onClick={onBack}>‹ Volver</button>
      <section className="glass-card auth-card">
        <div className="brand-logo">🐾</div><p className="eyebrow">Kindred Paws</p>
        <h1>{mode === 'invite' ? 'Acepta tu invitación' : 'Bienvenido de nuevo'}</h1>
        <p className="body-copy">{mode === 'invite' ? 'Inicia con Google para unirte a la comunidad autorizada.' : 'Los refugios construyen historias. Las familias encuentran un hogar.'}</p>
        {mode === 'login' ? <form onSubmit={submit} className="auth-form"><label>Usuario<input name="userName" autoComplete="username" required /></label><label>Contraseña<input name="password" type="password" autoComplete="current-password" required /></label><button className="primary-button" disabled={loading}>{loading ? 'Ingresando…' : 'Iniciar sesión'}</button></form> : <button className="primary-button" onClick={() => googleChallenge(invitationToken)}>Continuar con Google</button>}
        <div className="divider"><span>o</span></div><button className="secondary-button" onClick={() => window.location.assign(`${apiBaseUrl}/api/v1/auth/google/challenge`)}>Continuar con Google</button>
        {message && <p className="feedback" role="status">{message}</p>}
      </section>
    </main>
  )
}
