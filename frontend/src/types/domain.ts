export type Post = {
  id: string
  caption: string
  image: string
  shelter: string
  animal: string
  status: string
  likes: number
  comments: number
  createdAt: string
}

export type Story = {
  id: string
  name: string
  image: string
  viewed?: boolean
}

export type AuthResponse = {
  accessToken: string
  mustChangePassword: boolean
}
