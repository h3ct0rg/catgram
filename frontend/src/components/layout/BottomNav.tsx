type Props = {
  onHome: () => void
  onSearch: () => void
  onCreate: () => void
  onProfile: () => void
}

export function BottomNav({ onHome, onSearch, onCreate, onProfile }: Props) {
  return (
    <nav className="bottom-nav">
      <button className="active" onClick={onHome}>
        ⌂
        <small>Inicio</small>
      </button>
      <button onClick={onSearch}>
        ⌕
        <small>Buscar</small>
      </button>
      <button className="create" onClick={onCreate}>＋</button>
      <button onClick={onProfile}>
        ♙
        <small>Perfil</small>
      </button>
    </nav>
  )
}
