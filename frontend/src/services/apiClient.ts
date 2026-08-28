import { Animal, AuthResponse, Paginated, Post, Shelter, Story } from '../types/domain'
import { AdoptionRequest, AdoptionRequestStatus } from '../types/adoption'
import {
  Comment,
  FollowSummary,
  LikeSummary,
  Notification,
  NotificationPreference,
  NotificationType,
  ReportTargetType,
} from '../types/social'
import {
  AdminReport,
  AdminUser,
  AnimalStats,
  AuditAction,
  AuditLogEntry,
  DashboardSummary,
  ReportStatus,
  ShelterDashboardSummary,
} from '../types/admin'

export const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080'

const TOKEN_KEY = 'kindred_paws_access_token'
const REFRESH_TOKEN_KEY = 'kindred_paws_refresh_token'
export const TOKEN_REFRESHED_EVENT = 'kindred-paws:token-refreshed'

export function getAccessToken(): string | null {
  return sessionStorage.getItem(TOKEN_KEY)
}

export function setAccessToken(token: string | null) {
  if (token) sessionStorage.setItem(TOKEN_KEY, token)
  else sessionStorage.removeItem(TOKEN_KEY)
}

export function getRefreshToken(): string | null {
  return sessionStorage.getItem(REFRESH_TOKEN_KEY)
}

export function setRefreshToken(token: string | null) {
  if (token) sessionStorage.setItem(REFRESH_TOKEN_KEY, token)
  else sessionStorage.removeItem(REFRESH_TOKEN_KEY)
}

function storeAuthResponse(auth: AuthResponse) {
  setAccessToken(auth.accessToken)
  setRefreshToken(auth.refreshToken)
}

// Concurrent 401s (or the proactive timer firing alongside one) must not each rotate the refresh
// token independently — only the first caller performs the network call, everyone else awaits it.
let refreshInFlight: Promise<AuthResponse> | null = null

async function doRefresh(): Promise<AuthResponse> {
  const refreshToken = getRefreshToken()
  if (!refreshToken) throw new Error('No hay refresh token disponible.')

  const response = await fetch(`${apiBaseUrl}/api/v1/auth/refresh`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken }),
  })
  if (!response.ok) throw new Error('No se pudo renovar la sesión.')

  const auth = (await response.json()) as AuthResponse
  storeAuthResponse(auth)
  window.dispatchEvent(new CustomEvent<string>(TOKEN_REFRESHED_EVENT, { detail: auth.accessToken }))
  return auth
}

export function ensureFreshToken(): Promise<AuthResponse> {
  if (!refreshInFlight) {
    refreshInFlight = doRefresh().finally(() => {
      refreshInFlight = null
    })
  }
  return refreshInFlight
}

function clearSession() {
  setAccessToken(null)
  setRefreshToken(null)
}

async function request<T>(path: string, options: RequestInit = {}, isRetry = false): Promise<T> {
  const token = getAccessToken()
  const headers = new Headers(options.headers)
  if (token) headers.set('Authorization', `Bearer ${token}`)
  // Only force JSON for string bodies — a FormData body (multipart uploads) must keep the
  // browser-generated Content-Type (with its boundary), so leave it untouched here.
  if (options.body && typeof options.body === 'string' && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  const response = await fetch(`${apiBaseUrl}${path}`, { ...options, headers })

  if (response.status === 401) {
    // Try one silent refresh-and-retry before giving up — only if this isn't already a retry and
    // there's a refresh token to try with (a plain unauthenticated request has none).
    if (!isRetry && getRefreshToken()) {
      try {
        await ensureFreshToken()
        return await request<T>(path, options, true)
      } catch {
        // fall through to hard logout below
      }
    }
    clearSession()
    if (window.location.pathname !== '/expired') window.location.assign('/expired')
    throw new Error('Tu sesión terminó.')
  }

  if (!response.ok) {
    const problem = await response.json().catch(() => null)
    throw new Error(problem?.title ?? problem?.detail ?? 'Ocurrió un error inesperado.')
  }

  if (response.status === 204) return undefined as T
  const text = await response.text()
  return (text ? JSON.parse(text) : undefined) as T
}

// --- Auth ---

export function login(userName: string, password: string): Promise<AuthResponse> {
  return request<AuthResponse>('/api/v1/auth/login', {
    method: 'POST',
    body: JSON.stringify({ userName, password }),
  })
}

export function googleLogin(idToken: string, invitationToken?: string): Promise<AuthResponse> {
  return request<AuthResponse>('/api/v1/auth/google-login', {
    method: 'POST',
    body: JSON.stringify({ idToken, invitationToken }),
  })
}

export async function logoutRequest(): Promise<void> {
  const refreshToken = getRefreshToken()
  clearSession()
  if (!refreshToken) return
  try {
    await fetch(`${apiBaseUrl}/api/v1/auth/logout`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken }),
    })
  } catch {
    // best-effort: the client is already logged out locally regardless of network outcome
  }
}

// --- Feed ---

export async function getFeed(
  params: { cursor?: string | null; pageSize?: number; sort?: 'recent' | 'popular' } = {},
): Promise<Paginated<Post>> {
  const { cursor, pageSize = 10, sort = 'recent' } = params
  const query = new URLSearchParams({ pageSize: String(pageSize), sort })
  if (cursor) query.set(sort === 'popular' ? 'skip' : 'before', cursor)

  const items = await request<Post[]>(`/api/v1/social/feed?${query.toString()}`)
  const nextCursor =
    items.length < pageSize
      ? null
      : sort === 'popular'
        ? String((cursor ? Number(cursor) : 0) + pageSize)
        : (items[items.length - 1]?.createdAt ?? null)

  return { items, nextCursor }
}

export function getPost(postId: string): Promise<Post> {
  return request<Post>(`/api/v1/social/posts/${postId}`)
}

export function getStories(): Promise<Story[]> {
  return request<Story[]>('/api/v1/social/stories')
}

export async function viewStory(storyId: string, anonymousKey?: string): Promise<void> {
  await request<void>(`/api/v1/social/stories/${storyId}/views`, {
    method: 'POST',
    headers: anonymousKey ? { 'X-Anonymous-Key': anonymousKey } : undefined,
  })
}

// --- Animals ---

export function getAnimal(animalId: string): Promise<Animal> {
  return request<Animal>(`/api/v1/animals/${animalId}`)
}

export type CreateAnimalInput = {
  shelterId: string
  name: string
  species: string
  sex: string
  size: string
  ageMonths?: number
  breed?: string
  description: string
  location?: string
}

export function createAnimal(input: CreateAnimalInput): Promise<Animal> {
  return request<Animal>('/api/v1/animals', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export async function addAnimalMedia(
  animalId: string,
  file: File,
  isPrimary: boolean,
): Promise<void> {
  const form = new FormData()
  form.set('file', file)
  form.set('isPrimary', String(isPrimary))
  await request<void>(`/api/v1/animals/${animalId}/media`, { method: 'POST', body: form })
}

export type UpdateAnimalInput = {
  name: string
  species: string
  sex: string
  size: string
  ageMonths?: number
  breed?: string
  description: string
  location?: string
  adoptionStatus: string
}

export function updateAnimal(animalId: string, input: UpdateAnimalInput): Promise<Animal> {
  return request<Animal>(`/api/v1/animals/${animalId}`, {
    method: 'PUT',
    body: JSON.stringify(input),
  })
}

export async function deleteAnimal(animalId: string): Promise<void> {
  await request<void>(`/api/v1/animals/${animalId}`, { method: 'DELETE' })
}

// --- Likes ---

export function getLikeSummary(postId: string): Promise<LikeSummary> {
  return request<LikeSummary>(`/api/v1/posts/${postId}/likes`)
}

export async function like(postId: string): Promise<void> {
  await request<void>(`/api/v1/posts/${postId}/likes`, { method: 'PUT' })
}

export async function unlike(postId: string): Promise<void> {
  await request<void>(`/api/v1/posts/${postId}/likes`, { method: 'DELETE' })
}

// --- Comments ---

export function getComments(postId: string): Promise<Comment[]> {
  return request<Comment[]>(`/api/v1/posts/${postId}/comments`)
}

export function postComment(
  postId: string,
  body: string,
  parentCommentId?: string,
): Promise<Comment> {
  return request<Comment>(`/api/v1/posts/${postId}/comments`, {
    method: 'POST',
    body: JSON.stringify({ body, parentCommentId: parentCommentId ?? null }),
  })
}

export async function deleteComment(commentId: string): Promise<void> {
  await request<void>(`/api/v1/comments/${commentId}`, { method: 'DELETE' })
}

export async function likeComment(commentId: string): Promise<void> {
  await request<void>(`/api/v1/comments/${commentId}/likes`, { method: 'PUT' })
}

export async function unlikeComment(commentId: string): Promise<void> {
  await request<void>(`/api/v1/comments/${commentId}/likes`, { method: 'DELETE' })
}

// --- Follow ---

export function getFollowSummary(animalId: string): Promise<FollowSummary> {
  return request<FollowSummary>(`/api/v1/animals/${animalId}/follow`)
}

export async function follow(animalId: string): Promise<void> {
  await request<void>(`/api/v1/animals/${animalId}/follow`, { method: 'PUT' })
}

export async function unfollow(animalId: string): Promise<void> {
  await request<void>(`/api/v1/animals/${animalId}/follow`, { method: 'DELETE' })
}

// --- Reports ---

export async function createReport(
  targetType: ReportTargetType,
  targetId: string,
  reason: string,
): Promise<void> {
  await request<void>('/api/v1/reports', {
    method: 'POST',
    body: JSON.stringify({ targetType, targetId, reason }),
  })
}

// --- Notifications ---

export async function getNotifications(
  params: { cursor?: string | null; pageSize?: number; unreadOnly?: boolean } = {},
): Promise<Paginated<Notification>> {
  const { cursor, pageSize = 20, unreadOnly = false } = params
  const query = new URLSearchParams({ pageSize: String(pageSize), unreadOnly: String(unreadOnly) })
  if (cursor) query.set('before', cursor)
  const items = await request<Notification[]>(`/api/v1/notifications?${query.toString()}`)
  const nextCursor = items.length < pageSize ? null : (items[items.length - 1]?.createdAt ?? null)
  return { items, nextCursor }
}

export function getUnreadNotificationCount(): Promise<number> {
  return request<number>('/api/v1/notifications/unread-count')
}

export async function markNotificationRead(id: string): Promise<void> {
  await request<void>(`/api/v1/notifications/${id}/read`, { method: 'POST' })
}

export async function markAllNotificationsRead(): Promise<void> {
  await request<void>('/api/v1/notifications/read-all', { method: 'POST' })
}

export function getNotificationPreferences(): Promise<NotificationPreference[]> {
  return request<NotificationPreference[]>('/api/v1/notifications/preferences')
}

export async function updateNotificationPreference(
  type: NotificationType,
  enabled: boolean,
): Promise<void> {
  await request<void>(`/api/v1/notifications/preferences/${type}`, {
    method: 'PUT',
    body: JSON.stringify({ enabled }),
  })
}

// --- Shares (view/share counters used by the dashboard and per-animal stats) ---

export async function registerPostShare(postId: string): Promise<void> {
  await request<void>(`/api/v1/social/posts/${postId}/shares`, { method: 'POST' })
}

// --- Admin: users ---

export function getUsers(): Promise<AdminUser[]> {
  return request<AdminUser[]>('/api/v1/users')
}

export async function setUserStatus(userId: string, active: boolean): Promise<void> {
  await request<void>(`/api/v1/users/${userId}/status`, {
    method: 'PATCH',
    body: JSON.stringify({ active }),
  })
}

export async function assignUserRole(userId: string, role: string): Promise<void> {
  await request<void>(`/api/v1/users/${userId}/role`, {
    method: 'PUT',
    body: JSON.stringify({ role }),
  })
}

// --- Admin: moderation inbox ---

export function getReports(
  params: { status?: ReportStatus; targetType?: ReportTargetType } = {},
): Promise<AdminReport[]> {
  const query = new URLSearchParams()
  if (params.status) query.set('status', params.status)
  if (params.targetType) query.set('targetType', params.targetType)
  const qs = query.toString()
  return request<AdminReport[]>(`/api/v1/reports${qs ? `?${qs}` : ''}`)
}

export function resolveReport(id: string, status: ReportStatus): Promise<AdminReport> {
  return request<AdminReport>(`/api/v1/reports/${id}/resolve`, {
    method: 'POST',
    body: JSON.stringify({ status }),
  })
}

// --- Admin: audit log ---

export function getAuditLogs(
  params: { action?: AuditAction; entityType?: string; before?: string; pageSize?: number } = {},
): Promise<AuditLogEntry[]> {
  const query = new URLSearchParams({ pageSize: String(params.pageSize ?? 50) })
  if (params.action) query.set('action', params.action)
  if (params.entityType) query.set('entityType', params.entityType)
  if (params.before) query.set('before', params.before)
  return request<AuditLogEntry[]>(`/api/v1/audit-logs?${query.toString()}`)
}

// --- Admin: dashboard and per-animal stats ---

export function getDashboardSummary(): Promise<DashboardSummary> {
  return request<DashboardSummary>('/api/v1/dashboard/summary')
}

export function getShelterDashboardSummary(): Promise<ShelterDashboardSummary> {
  return request<ShelterDashboardSummary>('/api/v1/dashboard/my-shelter')
}

export function getAnimalStats(animalId: string): Promise<AnimalStats> {
  return request<AnimalStats>(`/api/v1/animals/${animalId}/stats`)
}

// --- Admin: invitations (SuperAdmin only) ---

export type CreateInvitationInput = {
  email: string
  fullName: string
  role: string
  shelterId?: string
  newShelterName?: string
}

export type Invitation = {
  id: string
  email: string
  fullName: string
  role: string
  shelterId: string | null
  shelterName: string | null
  newShelterName: string | null
  expiresAt: string
  status: 'Pendiente' | 'Aceptada' | 'Expirada'
}

export function createInvitation(input: CreateInvitationInput): Promise<Invitation> {
  return request<Invitation>('/api/v1/invitations', {
    method: 'POST',
    body: JSON.stringify({
      email: input.email,
      fullName: input.fullName,
      role: input.role,
      shelterId: input.shelterId ?? null,
      newShelterName: input.newShelterName ?? null,
    }),
  })
}

export function getInvitations(): Promise<Invitation[]> {
  return request<Invitation[]>('/api/v1/invitations')
}

export async function revokeInvitation(id: string): Promise<void> {
  await request<void>(`/api/v1/invitations/${id}`, { method: 'DELETE' })
}

export function resendInvitation(id: string): Promise<Invitation> {
  return request<Invitation>(`/api/v1/invitations/${id}/resend`, { method: 'POST' })
}

// --- My shelter (Administrador only) ---

export function getMyShelter(): Promise<Shelter> {
  return request<Shelter>('/api/v1/shelters/mine')
}

export type UpdateShelterInput = {
  name: string
  description: string
  address: string
  city: string
  country: string
  phone?: string
  whatsApp?: string
  email?: string
  latitude?: number
  longitude?: number
}

export function updateMyShelter(input: UpdateShelterInput): Promise<Shelter> {
  return request<Shelter>('/api/v1/shelters/mine', { method: 'PUT', body: JSON.stringify(input) })
}

// --- Discovery: search, filters, geolocation ---

export type AnimalSearchParams = {
  shelterId?: string
  name?: string
  species?: string
  sex?: string
  size?: string
  breed?: string
  location?: string
  adoptionStatus?: string
}

export function getAnimals(params: AnimalSearchParams = {}): Promise<Animal[]> {
  const query = new URLSearchParams()
  Object.entries(params).forEach(([key, value]) => {
    if (value) query.set(key, value)
  })
  const qs = query.toString()
  return request<Animal[]>(`/api/v1/animals${qs ? `?${qs}` : ''}`)
}

export function getNearbyAnimals(lat: number, lng: number, radiusKm = 25): Promise<Animal[]> {
  return request<Animal[]>(`/api/v1/animals/nearby?lat=${lat}&lng=${lng}&radiusKm=${radiusKm}`)
}

export function getShelters(name?: string): Promise<Shelter[]> {
  const query = name ? `?name=${encodeURIComponent(name)}` : ''
  return request<Shelter[]>(`/api/v1/shelters${query}`)
}

export function getShelter(shelterId: string): Promise<Shelter> {
  return request<Shelter>(`/api/v1/shelters/${shelterId}`)
}

// --- Adoption requests ---

export function createAdoptionRequest(
  animalId: string,
  answers: Record<string, string>,
): Promise<AdoptionRequest> {
  return request<AdoptionRequest>(`/api/v1/animals/${animalId}/adoption-requests`, {
    method: 'POST',
    body: JSON.stringify({ answers }),
  })
}

export function getMyAdoptionRequests(): Promise<AdoptionRequest[]> {
  return request<AdoptionRequest[]>('/api/v1/adoption-requests/mine')
}

export function getAdoptionRequests(
  params: { status?: AdoptionRequestStatus; animalId?: string } = {},
): Promise<AdoptionRequest[]> {
  const query = new URLSearchParams()
  if (params.status) query.set('status', params.status)
  if (params.animalId) query.set('animalId', params.animalId)
  const qs = query.toString()
  return request<AdoptionRequest[]>(`/api/v1/adoption-requests${qs ? `?${qs}` : ''}`)
}

export function updateAdoptionRequestStatus(
  id: string,
  status: AdoptionRequestStatus,
  reviewNotes?: string,
): Promise<AdoptionRequest> {
  return request<AdoptionRequest>(`/api/v1/adoption-requests/${id}/status`, {
    method: 'POST',
    body: JSON.stringify({ status, reviewNotes: reviewNotes ?? null }),
  })
}

// --- Post creation (admin) ---

export type CreatePostInput = {
  shelterId: string
  animalId: string
  caption: string
  location?: string
  hashtags?: string
  isFeatured?: boolean
  isSuccessStory?: boolean
  files: File[]
}

export function createPost(data: CreatePostInput): Promise<Post> {
  const form = new FormData()
  form.set('ShelterId', data.shelterId)
  form.set('AnimalId', data.animalId)
  form.set('Caption', data.caption)
  if (data.location) form.set('Location', data.location)
  if (data.hashtags) form.set('Hashtags', data.hashtags)
  form.set('IsFeatured', String(data.isFeatured ?? false))
  form.set('IsSuccessStory', String(data.isSuccessStory ?? false))
  data.files.forEach((file) => form.append('files', file))
  return request<Post>('/api/v1/social/posts', { method: 'POST', body: form })
}
