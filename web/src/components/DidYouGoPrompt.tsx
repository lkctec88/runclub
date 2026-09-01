import { useEffect, useState } from 'react'
import { activitiesApi } from '../api/services'
import { ApiError } from '../api/client'
import { ActivityKind, type ActivitySummary } from '../types'
import { activityHasEnded } from '../utils/activity'
import { RateActivityDialog } from './RateActivityDialog'

export function DidYouGoPrompt({
  activity,
  onUpdated,
}: {
  activity: ActivitySummary
  onUpdated?: () => void
}) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [rateOpen, setRateOpen] = useState(false)
  const [checkedIn, setCheckedIn] = useState(activity.myAttendance?.attended === true)
  const [missed, setMissed] = useState(activity.myAttendance?.attended === false)
  const storedRating = activity.myRating

  useEffect(() => {
    setCheckedIn(activity.myAttendance?.attended === true)
    setMissed(activity.myAttendance?.attended === false)
    setRateOpen(false)
  }, [activity.id, activity.myAttendance?.attended])

  if (
    !activityHasEnded(activity) ||
    activity.kind === ActivityKind.PersonalActivity
  ) {
    return null
  }

  const finishRating = () => {
    setRateOpen(false)
    onUpdated?.()
  }

  const confirm = async (didAttend: boolean) => {
    if (busy) return
    setBusy(true)
    setError('')
    try {
      await activitiesApi.confirmAttendance(activity.id, didAttend)
      if (didAttend) {
        setCheckedIn(true)
        setMissed(false)
        setRateOpen(true)
      } else {
        setCheckedIn(false)
        setMissed(true)
        onUpdated?.()
      }
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Could not save your answer')
    } finally {
      setBusy(false)
    }
  }

  if (missed) {
    return (
      <div className="did-you-go did-you-go--missed" onClick={(e) => e.stopPropagation()}>
        <p>I couldn't make it</p>
      </div>
    )
  }

  if (storedRating && !rateOpen) {
    return (
      <div className="did-you-go did-you-go--rated" onClick={(e) => e.stopPropagation()}>
        <p>You checked in</p>
        <div className="rating-stars rating-stars--saved" aria-label={`You rated this ${storedRating.overallRating} out of 5`}>
          {[1, 2, 3, 4, 5].map((value) => (
            <span
              key={value}
              className={`rating-star${value <= storedRating.overallRating ? ' is-filled' : ''}`}
            >
              ★
            </span>
          ))}
        </div>
        {storedRating.comments && <p className="activity-meta rating-saved-comment">{storedRating.comments}</p>}
      </div>
    )
  }

  if (checkedIn) {
    return (
      <div className="did-you-go" onClick={(e) => e.stopPropagation()}>
        <p>How was it?</p>
        <div className="did-you-go-actions">
          <button type="button" className="btn btn-primary" onClick={() => setRateOpen(true)}>
            Rate this activity
          </button>
        </div>
        {error && <p className="form-error">{error}</p>}
        {rateOpen && (
          <RateActivityDialog
            activityId={activity.id}
            activityTitle={activity.title}
            onSaved={finishRating}
            onSkip={finishRating}
            onDismiss={() => setRateOpen(false)}
          />
        )}
      </div>
    )
  }

  return (
    <div className="did-you-go" onClick={(e) => e.stopPropagation()}>
      <p>Did you go?</p>
      <div className="did-you-go-actions">
        <button
          type="button"
          className="btn btn-primary"
          disabled={busy}
          onClick={() => void confirm(true)}
        >
          Yes, I went
        </button>
        <button
          type="button"
          className="btn btn-outline"
          disabled={busy}
          onClick={() => void confirm(false)}
        >
          I didn't make it
        </button>
      </div>
      {error && <p className="form-error">{error}</p>}
    </div>
  )
}
