type Props = {
  title: string
  text: string
  action: string
  onAction: () => void
}

export function StateView({ title, text, action, onAction }: Props) {
  return (
    <main className="auth-shell">
      <section className="glass-card state-card">
        <div className="brand-logo">🐾</div>
        <h1>{title}</h1>
        <p className="body-copy">{text}</p>
        <button className="primary-button" onClick={onAction}>{action}</button>
      </section>
    </main>
  )
}
