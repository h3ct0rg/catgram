import { AuthResponse, Post } from '../types/domain'

export const apiBaseUrl =
  import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080'

export async function login(
  userName: string,
  password: string,
): Promise<AuthResponse> {
  const response = await fetch(`${apiBaseUrl}/api/v1/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ userName, password }),
  })

  if (!response.ok) {
    throw new Error('No fue posible iniciar sesión.')
  }

  return response.json() as Promise<AuthResponse>
}

export async function getFeed(): Promise<Post[]> {
  const response = await fetch(`${apiBaseUrl}/api/v1/social/feed?pageSize=20`)

  if (!response.ok) {
    return []
  }

  const items = (await response.json()) as Array<Record<string, unknown>>

  return items.map((item, index) => ({
    id: String(item.id ?? index),
    caption: String(item.caption ?? ''),
    image: String(
      (item.media as Array<{ url?: string }> | undefined)?.[0]?.url ?? '',
    ),
    shelter: 'Refugio Kindred Paws',
    animal: 'Animal',
    status: 'Disponible',
    likes: 0,
    comments: 0,
    createdAt: 'Reciente',
  }))
}

export function googleChallenge(invitationToken?: string) {
  const query = invitationToken
    ? `?invitationToken=${encodeURIComponent(invitationToken)}`
    : ''

  window.location.assign(
    `${apiBaseUrl}/api/v1/auth/google/challenge${query}`,
  )
}
