import { useEffect, useState } from 'react'
import { FeedPage } from '../features/feed/FeedPage'
import { AuthView } from '../features/auth/AuthView'
import { StateView } from '../features/auth/StateView'
import { PetView } from '../features/animals/PetView'
import { RegisterPetView } from '../features/animals/RegisterPetView'

export function App() {
  const [screen, setScreen] = useState(
    window.location.hash.replace('#', '') ||
      (new URLSearchParams(window.location.search).has('invitationToken')
        ? 'invite'
        : 'feed'),
  )

  useEffect(() => {
    const onHashChange = () => {
      setScreen(window.location.hash.replace('#', '') || 'feed')
    }
    window.addEventListener('hashchange', onHashChange)
    return () => window.removeEventListener('hashchange', onHashChange)
  }, [])

  function navigate(target: string) {
    window.location.hash = target
    setScreen(target)
  }

  if (screen === 'login' || screen === 'invite') {
    return <AuthView mode={screen} onBack={() => navigate('feed')} />
  }

  if (screen === 'expired') {
    return <StateView title="Tu sesión terminó" text="Vuelve a iniciar sesión para continuar disfrutando de las historias." action="Iniciar sesión" onAction={() => navigate('login')} />
  }

  if (screen === 'forbidden') {
    return <StateView title="No tienes acceso" text="Esta sección está reservada para usuarios autorizados por Kindred Paws." action="Volver al inicio" onAction={() => navigate('feed')} />
  }

  if (screen === 'pet') {
    return <PetView onBack={() => navigate('feed')} onAdopt={() => navigate('login')} />
  }

  if (screen === 'register') {
    return <RegisterPetView onBack={() => navigate('feed')} />
  }

  return <FeedPage onNavigate={navigate} />
}
