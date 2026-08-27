export function RegisterPetView({ onBack }: { onBack: () => void }) {
  return (
    <main className="register-shell">
      <button className="back-button" onClick={onBack}>‹ Volver</button>
      <section className="register-heading"><p className="eyebrow">Panel de refugio</p><h1>Registrar mascota</h1><p className="body-copy">Comparte su historia y ayúdalo a encontrar un hogar.</p></section>
      <form className="register-form" onSubmit={(event) => event.preventDefault()}>
        <label>Foto principal<div className="upload-box">＋<strong>Agregar foto</strong><small>JPG, PNG o WEBP</small></div></label>
        <label>Nombre<input placeholder="Ej. Luna" required /></label>
        <div className="two-columns"><label>Especie<select><option>Perro</option><option>Gato</option><option>Otro</option></select></label><label>Sexo<select><option>Macho</option><option>Hembra</option></select></label></div>
        <div className="two-columns"><label>Edad<input placeholder="Ej. 2 años" /></label><label>Tamaño<select><option>Mediano</option><option>Pequeño</option><option>Grande</option></select></label></div>
        <label>Descripción y personalidad<textarea rows={5} placeholder="Cuéntanos sobre sus gustos y personalidad…" /></label>
        <button className="primary-button" type="submit">🐾 &nbsp;Registrar mascota</button>
      </form>
    </main>
  )
}
