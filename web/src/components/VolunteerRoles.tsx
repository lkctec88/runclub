import { useEffect, useState, type MouseEvent } from 'react'
import { createPortal } from 'react-dom'
import { volunteerApi } from '../api/services'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import type { VolunteerSlot } from '../types'
import { ActivityKind, VolunteerSlotStatus, volunteerSlotLabel } from '../types'

interface VolunteerRolesProps {
  activityId: string
  runKind?: ActivityKind
  slots?: VolunteerSlot[]
  compact?: boolean
  showHeading?: boolean
  confirmRelease?: boolean
  onUpdated?: () => void
}

interface PendingSwitch {
  existing: VolunteerSlot
  target: VolunteerSlot
}

export function canHaveVolunteerSlots(kind?: ActivityKind) {
  return kind === ActivityKind.ClubActivity || kind === ActivityKind.Race
}

function VolunteerSwitchDialog({
  existingRole,
  newRole,
  loading,
  onConfirm,
  onCancel,
}: {
  existingRole: string
  newRole: string
  loading: boolean
  onConfirm: () => void
  onCancel: () => void
}) {
  const stopNav = (e: MouseEvent) => {
    e.stopPropagation()
  }

  return createPortal(
    <div
      className="dialog-backdrop"
      role="presentation"
      onClick={(e) => {
        stopNav(e)
        onCancel()
      }}
      onMouseDown={stopNav}
      onPointerDown={stopNav}
    >
      <div
        className="dialog-card"
        role="dialog"
        aria-modal="true"
        aria-labelledby="volunteer-switch-title"
        onClick={stopNav}
        onMouseDown={stopNav}
        onPointerDown={stopNav}
      >
        <h3 id="volunteer-switch-title" className="dialog-title">
          Change volunteer role?
        </h3>
        <p className="dialog-body">
          You have already volunteered for the <strong>{existingRole}</strong> role. Do you want to
          change to the <strong>{newRole}</strong> role?
        </p>
        <div className="dialog-actions">
          <button
            type="button"
            className="btn btn-outline"
            disabled={loading}
            onClick={(e) => {
              stopNav(e)
              onCancel()
            }}
          >
            Keep {existingRole}
          </button>
          <button
            type="button"
            className="btn btn-primary"
            disabled={loading}
            onClick={(e) => {
              stopNav(e)
              onConfirm()
            }}
          >
            {loading ? 'Updating…' : `Change to ${newRole}`}
          </button>
        </div>
      </div>
    </div>,
    document.body,
  )
}

export function VolunteerRoles({
  activityId,
  runKind,
  slots: initialSlots,
  compact = false,
  showHeading = false,
  confirmRelease,
  onUpdated,
}: VolunteerRolesProps) {
  const { user } = useAuth()
  const [slots, setSlots] = useState<VolunteerSlot[]>(initialSlots ?? [])
  const [loading, setLoading] = useState(initialSlots === undefined)
  const [claimingId, setClaimingId] = useState<string | null>(null)
  const [pendingSwitch, setPendingSwitch] = useState<PendingSwitch | null>(null)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')

  useEffect(() => {
    if (initialSlots !== undefined) {
      setSlots(initialSlots)
      setLoading(false)
      return
    }
    setLoading(true)
    volunteerApi
      .list(activityId)
      .then(setSlots)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Could not load volunteer roles'))
      .finally(() => setLoading(false))
  }, [activityId, initialSlots])

  const isMySlot = (slot: VolunteerSlot) =>
    !!user?.id &&
    slot.assignedUserId?.toLowerCase() === user.id.toLowerCase() &&
    slot.status === VolunteerSlotStatus.Claimed

  const mySlots = slots.filter(isMySlot)

  const applySlotUpdates = (released: VolunteerSlot, claimed: VolunteerSlot) => {
    setSlots((prev) =>
      prev.map((s) => {
        if (s.id === released.id) return released
        if (s.id === claimed.id) return claimed
        return s
      }),
    )
  }

  const performClaim = async (slotId: string) => {
    setClaimingId(slotId)
    setError('')
    setMessage('')
    try {
      const updated = await volunteerApi.claim(activityId, slotId)
      setSlots((prev) => prev.map((s) => (s.id === slotId ? updated : s)))
      setMessage('Signed up — thanks for volunteering!')
      onUpdated?.()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not sign up')
    } finally {
      setClaimingId(null)
    }
  }

  const performRelease = async (slot: VolunteerSlot) => {
    if ((confirmRelease ?? !compact) && !confirm(`Cancel your ${volunteerSlotLabel(slot)} sign-up?`)) return
    setClaimingId(slot.id)
    setError('')
    setMessage('')
    try {
      const updated = await volunteerApi.release(activityId, slot.id)
      setSlots((prev) => prev.map((s) => (s.id === slot.id ? updated : s)))
      setMessage(`You're no longer signed up for ${volunteerSlotLabel(slot)}.`)
      onUpdated?.()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not cancel sign-up')
    } finally {
      setClaimingId(null)
    }
  }

  const performSwitch = async () => {
    if (!pendingSwitch) return
    const { existing, target } = pendingSwitch
    setClaimingId(target.id)
    setError('')
    setMessage('')
    try {
      const released = await volunteerApi.release(activityId, existing.id)
      const claimed = await volunteerApi.claim(activityId, target.id)
      applySlotUpdates(released, claimed)
      setMessage(`You're now signed up for ${volunteerSlotLabel(target)}.`)
      setPendingSwitch(null)
      onUpdated?.()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not change volunteer role')
    } finally {
      setClaimingId(null)
    }
  }

  const requestClaim = (e: MouseEvent, slotId: string) => {
    e.stopPropagation()
    e.preventDefault()
    const target = slots.find((s) => s.id === slotId)
    if (!target || target.status !== VolunteerSlotStatus.Available) return

    const existing = mySlots[0]
    if (existing?.id === slotId) return

    if (existing) {
      setPendingSwitch({ existing, target })
      setError('')
      return
    }

    void performClaim(slotId)
  }

  if (runKind && !canHaveVolunteerSlots(runKind)) return null
  if (loading) return compact ? null : <p className="activity-meta">Loading volunteer roles…</p>
  if (slots.length === 0) return null

  const openSlots = slots.filter((s) => s.status === VolunteerSlotStatus.Available)

  const switchDialog = pendingSwitch ? (
    <VolunteerSwitchDialog
      existingRole={volunteerSlotLabel(pendingSwitch.existing)}
      newRole={volunteerSlotLabel(pendingSwitch.target)}
      loading={claimingId === pendingSwitch.target.id}
      onConfirm={() => void performSwitch()}
      onCancel={() => setPendingSwitch(null)}
    />
  ) : null

  if (openSlots.length === 0 && mySlots.length === 0) return null

  const picker = (
    <>
      {switchDialog}
      <div
        className="activity-volunteer-slots"
        onClick={(e) => e.stopPropagation()}
        onMouseDown={(e) => e.stopPropagation()}
        onPointerDown={(e) => e.stopPropagation()}
      >
        {openSlots.length > 0 && (
          <>
            <span className="activity-volunteer-label">Volunteer roles open</span>
            <ul className="activity-volunteer-list">
              {openSlots.map((slot) => (
                <li key={slot.id}>
                  <button
                    type="button"
                    className="activity-volunteer-chip activity-volunteer-chip--action"
                    disabled={claimingId === slot.id}
                    onClick={(e) => requestClaim(e, slot.id)}
                  >
                    {claimingId === slot.id ? 'Signing up…' : volunteerSlotLabel(slot)}
                  </button>
                </li>
              ))}
            </ul>
          </>
        )}
        {mySlots.length > 0 && (
          <div className="activity-volunteer-signed-up-row">
            <p className="activity-volunteer-signed-up">
              You're volunteering: {mySlots.map((s) => volunteerSlotLabel(s)).join(', ')}
            </p>
            {mySlots.map((slot) => (
              <button
                key={slot.id}
                type="button"
                className="btn btn-ghost btn-sm"
                style={{ color: 'var(--navy)' }}
                disabled={claimingId === slot.id}
                onClick={(e) => {
                  e.stopPropagation()
                  void performRelease(slot)
                }}
              >
                {claimingId === slot.id ? 'Cancelling…' : 'Cancel'}
              </button>
            ))}
          </div>
        )}
        {message && !compact && <p className="volunteer-message">{message}</p>}
        {error && <p className="form-error">{error}</p>}
      </div>
    </>
  )

  if (showHeading) {
    return (
      <section className="volunteer-section">
        <h2 className="volunteer-section-title">Volunteer</h2>
        {picker}
      </section>
    )
  }

  return picker
}
