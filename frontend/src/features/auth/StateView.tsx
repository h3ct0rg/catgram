import { useNavigate } from 'react-router-dom'

type Props = {
  title: string
  text: string
  action: string
  navigateTo: string
}

export function StateView({ title, text, action, navigateTo }: Props) {
  const navigate = useNavigate()
  return (
    <main className="auth-shell">
      <section className="glass-card state-card">
        <div className="brand-logo">🐾</div>
        <h1>{title}</h1>
        <p className="body-copy">{text}</p>
        <button className="primary-button" onClick={() => navigate(navigateTo)}>
          {action}
        </button>
      </section>
    </main>
  )
}
