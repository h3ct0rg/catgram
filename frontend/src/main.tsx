import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './styles.css'

function App() {
  return (
    <main className="shell">
      <section className="glass-card hero-card">
        <div className="brand-mark" aria-hidden="true">🐾</div>
        <p className="eyebrow">Kindred Paws</p>
        <h1>Historias que encuentran hogar.</h1>
        <p className="body-copy">
          La base de la experiencia pública está lista para conectar refugios,
          animales y familias.
        </p>
        <span className="status-pill">Fase 0 · Base preparada</span>
      </section>
    </main>
  )
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
