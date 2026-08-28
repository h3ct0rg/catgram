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

export type ShelterBreakdownItem = {
  shelterId: string
  shelterName: string
  animalCount: number
  adoptedCount: number
}

export type AnimalEngagementItem = {
  animalId: string
  animalName: string
  shelterName: string
  likes: number
  shares: number
  views: number
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
  sheltersBreakdown: ShelterBreakdownItem[]
  topAnimals: AnimalEngagementItem[]
}

export type ShelterDashboardSummary = {
  animals: number
  adoptedAnimals: number
  posts: number
  likes: number
  comments: number
  shares: number
  views: number
  pendingAdoptionRequests: number
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
