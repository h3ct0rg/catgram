type Props = {
  title: string
  message: string
  confirmLabel?: string
  danger?: boolean
  busy?: boolean
  error?: string
  onConfirm: () => void
  onCancel: () => void
}

export function ConfirmModal({
  title,
  message,
  confirmLabel = 'Confirmar',
  danger = false,
  busy = false,
  error,
  onConfirm,
  onCancel,
}: Props) {
  return (
    <div className="sheet-overlay" role="dialog" aria-modal="true" onClick={onCancel}>
      <div className="sheet" onClick={(event) => event.stopPropagation()}>
        <div className="sheet-handle" />
        <h2>{title}</h2>
        <p className="body-copy">{message}</p>
        {error && (
          <p className="feedback" role="status">
            {error}
          </p>
        )}
        <button
          className={danger ? 'danger-button' : 'primary-button'}
          disabled={busy}
          onClick={onConfirm}
        >
          {busy ? 'Procesando…' : confirmLabel}
        </button>
        <button className="secondary-button" disabled={busy} onClick={onCancel}>
          Cancelar
        </button>
      </div>
    </div>
  )
}
