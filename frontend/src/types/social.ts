export type Comment = {
  id: string
  postId: string
  authorId: string
  authorName: string
  parentCommentId: string | null
  body: string
  createdAt: string
  isMine: boolean
  likeCount: number
  likedByCurrentUser: boolean
}

export type LikeSummary = {
  count: number
  likedByCurrentUser: boolean
}

export type FollowSummary = {
  followerCount: number
  followedByCurrentUser: boolean
}

export type ReportTargetType = 'Post' | 'Comment' | 'User'

export const REPORT_REASONS = [
  'Contenido inapropiado',
  'Spam o publicidad',
  'Información falsa',
  'Acoso o lenguaje de odio',
  'Otro',
] as const

export type NotificationType = 'Like' | 'Comment' | 'Reply' | 'AdoptionStatusChanged' | 'NewPost'

export type Notification = {
  id: string
  type: NotificationType
  title: string
  body: string
  linkUrl: string | null
  relatedEntityId: string | null
  isRead: boolean
  createdAt: string
}

export type NotificationPreference = {
  type: NotificationType
  enabled: boolean
}
