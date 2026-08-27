const LABELS: Record<string, string> = {
  Available: 'Disponible',
  InProcess: 'En proceso de adopción',
  Adopted: 'Adoptado',
  Unavailable: 'No disponible',
  Deceased: 'Fallecido',
}

const COLORS: Record<string, string> = {
  Available: '#28a745',
  InProcess: '#f0a202',
  Adopted: '#2e5bff',
  Unavailable: '#d64545',
  Deceased: '#181c23',
}

const ICONS: Record<string, string> = {
  Available: '✓',
  InProcess: '…',
  Adopted: '♥',
  Unavailable: '✕',
  Deceased: '',
}

export function adoptionStatusLabel(status: string): string {
  return LABELS[status] ?? status
}

export function adoptionStatusColor(status: string): string {
  return COLORS[status] ?? '#717786'
}

export function adoptionStatusIcon(status: string): string {
  return ICONS[status] ?? ''
}
