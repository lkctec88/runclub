import { useEffect, useState, type MouseEvent } from 'react'
import { createPortal } from 'react-dom'
import { activitiesApi } from '../api/services'
import { ApiError } from '../api/client'

function stopNav(e: MouseEvent) {
  e.stopPropagation()
}

const scores = [1, 2, 3, 4, 5] as const

function StarIcon({ filled }: { filled: boolean }) {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true" className="rating-star-icon">
      <path
        d="M12 2.5l2.7 6.3 6.8.6-5.2 4.5 1.6 6.6L12 16.8 6.1 20.5l1.6-6.6L2.5 9.4l6.8-.6L12 2.5z"
        fill={filled ? 'currentColor' : 'none'}
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinejoin="round"
      />
    </svg>
  )
}

export function RateActivityDialog({
  activityId,
  activityTitle,
  onSaved,
  onSkip,
  onDismiss,
}: {
  activityId: string
  activityTitle: string
  onSaved: () => void
  onSkip: () => void
  onDismiss: () => void
}) {
  const [score, setScore] = useState<number | null>(null)
  const [hover, setHover] = useState<number | null>(null)
  const [comments, setComments] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && !busy) {
        if (saved) onSaved()
        else onDismiss()
      }
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [busy, saved, onSaved, onDismiss])

  const persist = async (value: number, text: string) => {
    await activitiesApi.rate(activityId, {
      overallRating: value,
      comments: text.trim() || null,
    })
    setSaved(true)
  }

  const chooseStar = async (value: number) => {
    if (busy) return
    setScore(value)
    setBusy(true)
    setError('')
    try {
      await persist(value, comments)
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Could not save your rating')
    } finally {
      setBusy(false)
    }
  }

  const submit = async () => {
    if (score == null || busy) return
    setBusy(true)
    setError('')
    try {
      await persist(score, comments)
      onSaved()
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Could not save your rating')
      setBusy(false)
    }
  }

  const skip = async () => {
    if (busy) return
    if (saved) {
      onSaved()
      return
    }
    setBusy(true)
    setError('')
    try {
      await activitiesApi.skipRating(activityId)
      onSkip()
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Could not skip rating')
      setBusy(false)
    }
  }

  return createPortal(
    <div
      className="dialog-backdrop"
      role="presentation"
      onClick={(e) => {
        stopNav(e)
        if (busy) return
        if (saved) onSaved()
        else onDismiss()
      }}
      onMouseDown={stopNav}
      onPointerDown={stopNav}
    >
      <div
        className="dialog-card rating-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="rating-dialog-title"
        onClick={stopNav}
        onMouseDown={stopNav}
        onPointerDown={stopNav}
      >
        <h3 id="rating-dialog-title" className="dialog-title">
          How was it?
        </h3>
        <p className="dialog-body going-dialog-subtitle">{activityTitle}</p>
        <p className="rating-label">Tap a star to rate</p>
        <div
          className="rating-stars"
          role="group"
          aria-label="Rating from 1 to 5 stars"
          onMouseLeave={() => setHover(null)}
        >
          {scores.map((value) => {
            const filled = value <= (hover ?? score ?? 0)
            return (
              <button
                key={value}
                type="button"
                className={`rating-star${filled ? ' is-filled' : ''}`}
                aria-label={`${value} star${value === 1 ? '' : 's'}`}
                aria-pressed={score === value}
                disabled={busy}
                onMouseEnter={() => setHover(value)}
                onClick={(e) => {
                  stopNav(e)
                  void chooseStar(value)
                }}
              >
                <StarIcon filled={filled} />
              </button>
            )
          })}
        </div>
        <div className="form-group">
          <label htmlFor="rating-comments">Anything to add? (optional)</label>
          <textarea
            id="rating-comments"
            rows={3}
            value={comments}
            disabled={busy}
            placeholder="What went well, or what could be better?"
            onChange={(e) => setComments(e.target.value)}
          />
        </div>
        {error && <p className="form-error">{error}</p>}
        <div className="dialog-actions">
          <button
            type="button"
            className="btn btn-primary"
            disabled={busy || score == null}
            onClick={(e) => {
              stopNav(e)
              void submit()
            }}
          >
            {busy ? 'Saving…' : 'Send rating'}
          </button>
          <button
            type="button"
            className="btn btn-outline"
            disabled={busy}
            onClick={(e) => {
              stopNav(e)
              void skip()
            }}
          >
            Skip
          </button>
        </div>
      </div>
    </div>,
    document.body,
  )
}
