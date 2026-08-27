import { fallbackPosts } from '../feed/feedData'

type Props = { onBack: () => void; onAdopt: () => void }

export function PetView({ onBack, onAdopt }: Props) {
  return (
    <main className="pet-shell">
      <button className="back-button" onClick={onBack}>‹ Volver</button>
      <section className="pet-hero"><img src={fallbackPosts[0].image} alt="Rocky jugando" /><span className="status">✓ Disponible</span><h1>Rocky</h1></section>
      <section className="pet-grid"><div><small>RAZA</small><strong>Border Collie Mix</strong></div><div><small>EDAD</small><strong>3 años</strong></div><div><small>TAMAÑO</small><strong>Mediano</strong></div><div><small>SEXO</small><strong>Macho</strong></div></section>
      <section className="glass-card about"><h2>ⓘ &nbsp;Acerca de Rocky</h2><p>Rocky es increíblemente energético y cariñoso. Le encantan las caminatas largas, jugar a buscar y espera una familia para siempre.</p></section>
      <h2 className="section-title">Galería</h2><div className="gallery"><img src={fallbackPosts[0].image} alt="Rocky en el parque" /><img src={fallbackPosts[1].image} alt="Rocky descansando" /></div>
      <div className="location glass-card"><h2>⌖ &nbsp;Ubicación</h2><p>Happy Paws Shelter, Austin, TX</p></div><button className="primary-button adopt-button" onClick={onAdopt}>♡ &nbsp;Adoptar a Rocky</button>
    </main>
  )
}
