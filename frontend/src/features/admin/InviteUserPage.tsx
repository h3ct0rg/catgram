import { FormEvent, useEffect, useState } from 'react'
import {
  createInvitation,
  getInvitations,
  getShelters,
  Invitation,
  resendInvitation,
  revokeInvitation,
} from '../../services/apiClient'
import { Shelter } from '../../types/domain'

const ROLES = ['Usuario', 'Administrador']

export function InviteUserPage() {
  const [showForm, setShowForm] = useState(false)
  const [email, setEmail] = useState('')
  const [fullName, setFullName] = useState('')
  const [role, setRole] = useState('Usuario')
  const [shelterMode, setShelterMode] = useState<'existing' | 'new'>('new')
  const [shelterId, setShelterId] = useState('')
  const [newShelterName, setNewShelterName] = useState('')
  const [shelters, setShelters] = useState<Shelter[]>([])
  const [invitations, setInvitations] = useState<Invitation[]>([])
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [busyId, setBusyId] = useState('')
  const [error, setError] = useState('')

  useEffect(() => {
    getShelters()
      .then(setShelters)
      .catch(() => undefined)
    getInvitations()
      .then(setInvitations)
      .catch(() => setError('No se pudieron cargar las invitaciones.'))
      .finally(() => setLoading(false))
  }, [])

  function resetForm() {
    setEmail('')
    setFullName('')
    setRole('Usuario')
    setShelterMode('new')
    setShelterId('')
    setNewShelterName('')
  }

  async function submit(event: FormEvent) {
    event.preventDefault()
    setSubmitting(true)
    setError('')
    try {
      const invitation = await createInvitation({
        email,
        fullName,
        role,
        shelterId: role === 'Administrador' && shelterMode === 'existing' ? shelterId : undefined,
        newShelterName:
          role === 'Administrador' && shelterMode === 'new' ? newShelterName : undefined,
      })
      setInvitations((current) => [invitation, ...current])
      resetForm()
      setShowForm(false)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo crear la invitación.')
    } finally {
      setSubmitting(false)
    }
  }

  async function resend(invitation: Invitation) {
    setBusyId(invitation.id)
    setError('')
    try {
      const updated = await resendInvitation(invitation.id)
      setInvitations((current) => current.map((x) => (x.id === updated.id ? updated : x)))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo reenviar la invitación.')
    } finally {
      setBusyId('')
    }
  }

  async function remove(invitation: Invitation) {
    if (!window.confirm(`¿Eliminar la invitación de ${invitation.fullName}?`)) return
    setBusyId(invitation.id)
    setError('')
    try {
      await revokeInvitation(invitation.id)
      setInvitations((current) => current.filter((x) => x.id !== invitation.id))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo eliminar la invitación.')
    } finally {
      setBusyId('')
    }
  }

  return (
    <div>
      <div className="section-header">
        <h2 className="section-title">Invitaciones</h2>
        <button
          className="primary-button"
          type="button"
          onClick={() => setShowForm((current) => !current)}
        >
          {showForm ? 'Cancelar' : 'Crear invitación'}
        </button>
      </div>

      {showForm && (
        <form className="register-form" onSubmit={submit}>
          <label>
            Correo
            <input
              type="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              required
            />
          </label>
          <label>
            Nombre completo
            <input value={fullName} onChange={(event) => setFullName(event.target.value)} required />
          </label>
          <label>
            Rol
            <select value={role} onChange={(event) => setRole(event.target.value)}>
              {ROLES.map((option) => (
                <option key={option} value={option}>
                  {option}
                </option>
              ))}
            </select>
          </label>
          {role === 'Administrador' && (
            <>
              <div className="two-columns">
                <label className="checkbox-row">
                  <input
                    type="radio"
                    name="shelterMode"
                    checked={shelterMode === 'new'}
                    onChange={() => setShelterMode('new')}
                  />
                  Refugio nuevo
                </label>
                <label className="checkbox-row">
                  <input
                    type="radio"
                    name="shelterMode"
                    checked={shelterMode === 'existing'}
                    onChange={() => setShelterMode('existing')}
                  />
                  Refugio existente
                </label>
              </div>
              {shelterMode === 'new' ? (
                <label>
                  Nombre del refugio nuevo
                  <input
                    value={newShelterName}
                    onChange={(event) => setNewShelterName(event.target.value)}
                    required
                  />
                </label>
              ) : (
                <label>
                  Refugio existente
                  <select
                    value={shelterId}
                    onChange={(event) => setShelterId(event.target.value)}
                    required
                  >
                    <option value="">Selecciona un refugio</option>
                    {shelters.map((shelter) => (
                      <option key={shelter.id} value={shelter.id}>
                        {shelter.name}
                      </option>
                    ))}
                  </select>
                </label>
              )}
            </>
          )}
          <button className="primary-button" type="submit" disabled={submitting}>
            {submitting ? 'Enviando…' : 'Enviar invitación'}
          </button>
        </form>
      )}

      {error && (
        <p className="feedback" role="status">
          {error}
        </p>
      )}

      {loading && <p className="body-copy">Cargando…</p>}
      <div className="admin-table">
        {invitations.map((invitation) => (
          <div className="admin-row" key={invitation.id}>
            <div>
              <strong>{invitation.fullName}</strong> · {invitation.email}
              <p>
                {invitation.role}
                {invitation.shelterName ? ` · ${invitation.shelterName}` : ''}
                {invitation.newShelterName ? ` · ${invitation.newShelterName} (nuevo)` : ''}
              </p>
              <small>Expira: {new Date(invitation.expiresAt).toLocaleString('es-ES')}</small>
            </div>
            <div className="admin-row-actions">
              <span className={`badge badge-${invitation.status.toLowerCase()}`}>
                {invitation.status}
              </span>
              {invitation.status !== 'Aceptada' && (
                <>
                  <button
                    className="secondary-button"
                    disabled={busyId === invitation.id}
                    onClick={() => resend(invitation)}
                  >
                    Reenviar
                  </button>
                  <button
                    className="danger-button"
                    disabled={busyId === invitation.id}
                    onClick={() => remove(invitation)}
                  >
                    Eliminar
                  </button>
                </>
              )}
            </div>
          </div>
        ))}
        {!loading && invitations.length === 0 && (
          <p className="body-copy">No hay invitaciones todavía.</p>
        )}
      </div>
    </div>
  )
}
