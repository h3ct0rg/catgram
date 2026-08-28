import { BrowserRouter, Navigate, Route, Routes, useSearchParams } from 'react-router-dom'
import { FeedPage } from '../features/feed/FeedPage'
import { PostDetailPage } from '../features/feed/PostDetailPage'
import { StoryViewer } from '../features/feed/StoryViewer'
import { AuthView } from '../features/auth/AuthView'
import { StateView } from '../features/auth/StateView'
import { PetView } from '../features/animals/PetView'
import { RegisterPetView } from '../features/animals/RegisterPetView'
import { NotificationsPage } from '../features/social/NotificationsPage'
import { AdminLayout } from '../features/admin/AdminLayout'
import { DashboardPage } from '../features/admin/DashboardPage'
import { ReportsInboxPage } from '../features/admin/ReportsInboxPage'
import { UsersPage } from '../features/admin/UsersPage'
import { AuditLogPage } from '../features/admin/AuditLogPage'
import { AdoptionRequestsPage } from '../features/admin/AdoptionRequestsPage'
import { CreatePostPage } from '../features/admin/CreatePostPage'
import { InviteUserPage } from '../features/admin/InviteUserPage'
import { MyShelterPage } from '../features/admin/MyShelterPage'
import { PetsPage } from '../features/admin/PetsPage'
import { SearchPage } from '../features/discovery/SearchPage'
import { RequireRole } from './RequireRole'

const ADMIN_ROLES = ['Administrador', 'SuperAdministrador']
const SUPER_ADMIN_ONLY = ['SuperAdministrador']
const ADMINISTRADOR_ONLY = ['Administrador']

function RootRoute() {
  const [params] = useSearchParams()
  const invitationToken = params.get('invitationToken')
  if (invitationToken) {
    return (
      <Navigate to={`/invite?invitationToken=${encodeURIComponent(invitationToken)}`} replace />
    )
  }
  return <FeedPage />
}

export function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<RootRoute />} />
        <Route path="/login" element={<AuthView mode="login" />} />
        <Route path="/invite" element={<AuthView mode="invite" />} />
        <Route
          path="/expired"
          element={
            <StateView
              title="Tu sesión terminó"
              text="Vuelve a iniciar sesión para continuar disfrutando de las historias."
              action="Iniciar sesión"
              navigateTo="/login"
            />
          }
        />
        <Route
          path="/forbidden"
          element={
            <StateView
              title="No tienes acceso"
              text="Esta sección está reservada para usuarios autorizados por Kindred Paws."
              action="Volver al inicio"
              navigateTo="/"
            />
          }
        />
        <Route path="/animals/new" element={<RegisterPetView />} />
        <Route path="/animals/:animalId/edit" element={<RegisterPetView />} />
        <Route path="/animals/:animalId" element={<PetView />} />
        <Route path="/p/:postId" element={<PostDetailPage />} />
        <Route path="/stories/:storyId" element={<StoryViewer />} />
        <Route path="/notifications" element={<NotificationsPage />} />
        <Route path="/search" element={<SearchPage />} />
        <Route
          path="/admin"
          element={
            <RequireRole roles={ADMIN_ROLES}>
              <AdminLayout />
            </RequireRole>
          }
        >
          <Route index element={<DashboardPage />} />
          <Route path="reports" element={<ReportsInboxPage />} />
          <Route path="adoptions" element={<AdoptionRequestsPage />} />
          <Route path="posts/new" element={<CreatePostPage />} />
          <Route
            path="users"
            element={
              <RequireRole roles={SUPER_ADMIN_ONLY}>
                <UsersPage />
              </RequireRole>
            }
          />
          <Route
            path="audit"
            element={
              <RequireRole roles={SUPER_ADMIN_ONLY}>
                <AuditLogPage />
              </RequireRole>
            }
          />
          <Route
            path="invite"
            element={
              <RequireRole roles={SUPER_ADMIN_ONLY}>
                <InviteUserPage />
              </RequireRole>
            }
          />
          <Route
            path="shelter"
            element={
              <RequireRole roles={ADMINISTRADOR_ONLY}>
                <MyShelterPage />
              </RequireRole>
            }
          />
          <Route
            path="pets"
            element={
              <RequireRole roles={ADMINISTRADOR_ONLY}>
                <PetsPage />
              </RequireRole>
            }
          />
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
