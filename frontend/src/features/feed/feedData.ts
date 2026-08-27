import { Post, Story } from '../../types/domain'

export const fallbackPosts: Post[] = [
  {
    id: 'rocky',
    caption:
      'Rocky jugando en el parque hoy. Tiene 3 años, está vacunado y listo para su hogar para siempre. 🐾❤️',
    image:
      'https://lh3.googleusercontent.com/aida-public/AB6AXuCQT4RfokGgt_6tg2v-uIm8M184gcAG_6Pd8IBd16G_F7a-_UbWDV3Be0z_Fkc2BDsKAV_D1Whz_kfUYagYB5h1YLf2NbqCM4bqmyVghzYhvYcyVSkrTOE9zLkKiNFEuOsLcX1ATxsiCd2CIUX7S1i-BiUNAv5YLNUMLtTRA1lwx5irF5YfaH8e8i6iCHa-xbg8pwVBhQEXEDnmMkK26iY3vgK6mRqWY3XAR4pBQNpZQXKNGz7Qw_mtcQ',
    shelter: 'Happy Paws Shelter',
    animal: 'Rocky',
    status: 'Disponible',
    likes: 245,
    comments: 12,
    createdAt: 'Hace 2 horas',
  },
  {
    id: 'luna',
    caption:
      'Luna necesita un hogar. Es dulce, tranquila y le encantan los paseos y los abrazos. 🐺✨',
    image:
      'https://lh3.googleusercontent.com/aida-public/AB6AXuDuHD8Hnw_i4w3GK9Gz75cs1SCYFfYVxF_EirAWTxu_Jw2TeT-44DEwLwuFVO7NTRBzQdM1KdPn_2TNFIl9hINpLZXSmdeTyQ58cyTooXhzHroYTkbaK-vWDAkKqhTI9IbuEFw6YA1jV2bcGuFU45q50NfjNoq4kEOy4K5sZS8XMTT3ylJLaYvuWzg0tFzo8luLpueniOSN6_h4H5TdsC7favHv9WfPO9UXpemjIFcdBW6oqn7hJJcVmQ',
    shelter: 'City Rescue',
    animal: 'Luna',
    status: 'Solicitud en curso',
    likes: 89,
    comments: 4,
    createdAt: 'Hace 5 horas',
  },
]

export const stories: Story[] = [
  { id: 'bella', name: 'Bella', image: fallbackPosts[0].image },
  { id: 'shadow', name: 'Shadow', image: fallbackPosts[1].image },
  { id: 'pongo', name: 'Pongo', image: fallbackPosts[0].image },
  { id: 'bunny', name: 'Bunny', image: fallbackPosts[1].image, viewed: true },
]
