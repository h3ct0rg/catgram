import { NotificationBell } from '../social/NotificationBell'

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
      <div className="topbar-actions">
        <NotificationBell />
        <button className="avatar" aria-label="Abrir perfil" onClick={onProfile}>
          👩🏻
        </button>
      </div>
    </header>
  )
}
