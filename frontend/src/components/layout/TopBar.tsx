type Props = {
  onHome: () => void
  onProfile: () => void
}

export function TopBar({ onHome, onProfile }: Props) {
  return (
    <header className="topbar">
      <button className="brand" onClick={onHome}>
        <span className="paw">🐾</span>
        <span>Kindred Paws</span>
      </button>
      <button className="avatar" aria-label="Abrir perfil" onClick={onProfile}>
        👩🏻
      </button>
    </header>
  )
}
