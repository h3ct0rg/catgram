export type AnimalMedia = {
  id: string
  url: string
  thumbnailUrl: string | null
  contentType: string
  isPrimary: boolean
}

export type Post = {
  id: string
  shelterId: string
  shelterName: string
  animalId: string
  animalName: string
  adoptionStatus: string
  caption: string
  location: string | null
  hashtags: string | null
  isFeatured: boolean
  createdAt: string
  likeCount: number
  commentCount: number
  likedByCurrentUser: boolean
  media: AnimalMedia[]
}

export type Story = {
  id: string
  shelterId: string
  animalId: string
  animalName: string
  caption: string
  mediaUrl: string
  contentType: string
  createdAt: string
  expiresAt: string
  views: number
}

export type Animal = {
  id: string
  shelterId: string
  shelterName: string
  name: string
  species: string
  sex: string
  size: string
  ageMonths: number | null
  breed: string | null
  description: string
  adoptionStatus: string
  location: string | null
  media: AnimalMedia[]
}

export type Paginated<T> = {
  items: T[]
  nextCursor: string | null
}

export type AuthResponse = {
  accessToken: string
  expiresAt: string
  userName: string
  roles: string[]
  mustChangePassword: boolean
}
