export type AdoptionRequestStatus = 'Pending' | 'InReview' | 'Approved' | 'Rejected' | 'Completed'

export type AdoptionRequest = {
  id: string
  animalId: string
  animalName: string
  applicantUserId: string
  applicantUserName: string
  status: AdoptionRequestStatus
  answers: Record<string, string>
  reviewNotes: string | null
  createdAt: string
  updatedAt: string
}
