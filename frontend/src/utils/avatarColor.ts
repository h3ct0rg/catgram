const PALETTE = [
  '#0059bb',
  '#2e5bff',
  '#8a3ffc',
  '#d64545',
  '#f0a202',
  '#28a745',
  '#ff6f91',
  '#00b8a9',
]

export function getInitials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0) return '?'
  if (parts.length === 1) return parts[0]!.slice(0, 2).toUpperCase()
  return (parts[0]![0] + parts[1]![0]).toUpperCase()
}

export function stringToColor(seed: string): string {
  let hash = 0
  for (let i = 0; i < seed.length; i++) hash = seed.charCodeAt(i) + ((hash << 5) - hash)
  return PALETTE[Math.abs(hash) % PALETTE.length]!
}
