import { useEffect, useState, type MouseEvent } from 'react'
import { createPortal } from 'react-dom'
import { activitiesApi } from '../api/services'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import type { GoingMember } from '../types'

function stopNav(e: MouseEvent) {
  e.stopPropagation()
}

export function GoingPeople({
  activityId,
  runTitle,
  goingCount,
  people: initialPeople,
}: {
  activityId: string
  runTitle: string
  goingCount?: number
  people?: GoingMember[]
}) {
  const { user } = useAuth()
  const [open, setOpen] = useState(false)
  const [people, setPeople] = useState<GoingMember[] | null>(initialPeople ?? null)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!open) return
    if (Array.isArray(initialPeople)) {
      setPeople(initialPeople)
      setError('')
      return
    }
    setError('')
    setPeople(null)
    activitiesApi
      .listGoing(activityId)
      .then(setPeople)
      .catch((e) => setError(e instanceof ApiError ? e.message : 'Could not load who is going'))
  }, [open, activityId, initialPeople])

  useEffect(() => {
    if (!open) return
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setOpen(false)
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [open])

  const count = goingCount ?? people?.length ?? 0
  const label = `${count} going`

  return (
    <>
      <button
        type="button"
        className="activity-going-count"
        aria-haspopup="dialog"
        aria-expanded={open}
        onClick={(e) => {
          stopNav(e)
          setOpen(true)
        }}
      >
        {label}
      </button>
      {open &&
        createPortal(
          <div
            className="dialog-backdrop"
            role="presentation"
            onClick={(e) => {
              stopNav(e)
              setOpen(false)
            }}
            onMouseDown={stopNav}
            onPointerDown={stopNav}
          >
            <div
              className="dialog-card going-dialog"
              role="dialog"
              aria-modal="true"
              aria-labelledby="going-dialog-title"
              onClick={stopNav}
              onMouseDown={stopNav}
              onPointerDown={stopNav}
            >
              <h3 id="going-dialog-title" className="dialog-title">
                Going
              </h3>
              <p className="dialog-body going-dialog-subtitle">{runTitle}</p>
              {error && <p className="form-error">{error}</p>}
              {!error && people === null && <p className="activity-meta">Loading…</p>}
              {!error && people && people.length === 0 && (
                <p className="activity-meta">Nobody has said they are going yet.</p>
              )}
              {people && people.length > 0 && (
                <ul className="going-list">
                  {people.map((person) => {
                    const isYou = person.userId === user?.id
                    return (
                      <li key={person.userId} className="going-list-item">
                        <span className="going-list-name">
                          {person.firstName} {person.lastName}
                          {isYou && <span className="going-you">You</span>}
                        </span>
                        {person.typicalPace && (
                          <span className="activity-meta">{person.typicalPace}</span>
                        )}
                      </li>
                    )
                  })}
                </ul>
              )}
              <div className="dialog-actions">
                <button
                  type="button"
                  className="btn btn-outline"
                  onClick={(e) => {
                    stopNav(e)
                    setOpen(false)
                  }}
                >
                  Close
                </button>
              </div>
            </div>
          </div>,
          document.body,
        )}
    </>
  )
}
