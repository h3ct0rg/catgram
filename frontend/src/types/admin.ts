import { ReportTargetType } from './social'

export type AdminUser = {
  id: string
  userName: string
  email: string
  fullName: string
  isActive: boolean
  roles: string[]
}

export type ReportStatus = 'Pending' | 'Reviewed' | 'Resolved' | 'Dismissed'

export type AdminReport = {
  id: string
  reporterId: string
  targetType: ReportTargetType
  targetId: string
  reason: string
  status: ReportStatus
  createdAt: string
}

export type AuditAction =
  | 'UserActivated'
  | 'UserDeactivated'
  | 'UserRoleChanged'
  | 'PostHidden'
  | 'CommentHidden'
  | 'AdoptionStatusChanged'
  | 'ReportResolved'

export type AuditLogEntry = {
  id: string
  actorUserId: string
  actorUserName: string
  action: AuditAction
  entityType: string
  entityId: string
  details: string | null
  createdAt: string
}

export type DashboardSummary = {
  users: number
  shelters: number
  animals: number
  adoptedAnimals: number
  posts: number
  activeStories: number
  likes: number
  comments: number
  shares: number
  views: number
}

export type AnimalStats = {
  animalId: string
  animalName: string
  adoptionStatus: string
  postCount: number
  totalLikes: number
  totalComments: number
  totalViews: number
  totalShares: number
  followerCount: number
}
