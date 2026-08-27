import { Animal, AuthResponse, Paginated, Post, Story } from '../types/domain'
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
} from '../types/admin'

export const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080'

const TOKEN_KEY = 'kindred_paws_access_token'

export function getAccessToken(): string | null {
  return sessionStorage.getItem(TOKEN_KEY)
}

export function setAccessToken(token: string | null) {
  if (token) sessionStorage.setItem(TOKEN_KEY, token)
  else sessionStorage.removeItem(TOKEN_KEY)
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = getAccessToken()
  const headers = new Headers(options.headers)
  if (token) headers.set('Authorization', `Bearer ${token}`)
  if (options.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')

  const response = await fetch(`${apiBaseUrl}${path}`, { ...options, headers })

  if (response.status === 401) {
    setAccessToken(null)
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

export async function login(userName: string, password: string): Promise<AuthResponse> {
  return request<AuthResponse>('/api/v1/auth/login', {
    method: 'POST',
    body: JSON.stringify({ userName, password }),
  })
}

export function googleChallenge(invitationToken?: string) {
  const query = invitationToken ? `?invitationToken=${encodeURIComponent(invitationToken)}` : ''
  window.location.assign(`${apiBaseUrl}/api/v1/auth/google/challenge${query}`)
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

export function getAnimalStats(animalId: string): Promise<AnimalStats> {
  return request<AnimalStats>(`/api/v1/animals/${animalId}/stats`)
}
