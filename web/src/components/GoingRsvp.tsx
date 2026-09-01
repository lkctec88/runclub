import { useEffect, useState } from 'react'
import { activitiesApi } from '../api/services'
import { ApiError } from '../api/client'
import { AttendanceStatus, type ActivitySummary } from '../types'
import { isRsvpGoing, activityHasEnded } from '../utils/activity'

export function GoingRsvp({
  activity,
  onUpdated,
  showJoin = false,
}: {
  activity: ActivitySummary
  onUpdated?: () => void
  showJoin?: boolean
}) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [going, setGoing] = useState(() => isRsvpGoing(activity.myAttendance))

  useEffect(() => {
    setGoing(isRsvpGoing(activity.myAttendance))
  }, [activity.id])

  useEffect(() => {
    if (activity.myAttendance != null) setGoing(isRsvpGoing(activity.myAttendance))
  }, [activity.myAttendance])

  if (activityHasEnded(activity)) return null
  if (!going && !showJoin) return null

  const setStatus = async (status: AttendanceStatus) => {
    if (busy) return
    setBusy(true)
    setError('')
    try {
      await activitiesApi.setAttendance(activity.id, status)
      setGoing(status === AttendanceStatus.Going)
      onUpdated?.()
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Could not update attendance')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="activity-going-rsvp" onClick={(e) => e.stopPropagation()}>
      {going ? (
        <div className="activity-going-confirmed">
          <p>You're going</p>
          <button
            type="button"
            className="btn btn-outline btn-sm"
            disabled={busy}
            onClick={(e) => {
              e.stopPropagation()
              void setStatus(AttendanceStatus.NotGoing)
            }}
          >
            {busy ? 'Cancelling…' : 'Cancel'}
          </button>
        </div>
      ) : (
        <button
          type="button"
          className="btn btn-primary"
          disabled={busy}
          onClick={() => void setStatus(AttendanceStatus.Going)}
        >
          {busy ? 'Saving…' : "I'm going"}
        </button>
      )}
      {error && <p className="form-error">{error}</p>}
    </div>
  )
}
