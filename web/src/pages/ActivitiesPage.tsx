import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { activitiesApi } from '../api/services'
import { ActivityCard } from '../components/ActivityCard'
import { useAuth } from '../auth/AuthContext'
import type { ActivitySummary } from '../types'
import { isCurrentActivity, isPastActivity, needsAttendanceConfirm } from '../utils/activity'

type ListView = 'current' | 'past'

export function ActivitiesPage() {
  const { clubId } = useAuth()
  const [activities, setActivities] = useState<ActivitySummary[]>([])
  const [loading, setLoading] = useState(true)
  const [view, setView] = useState<ListView>('current')

  const load = () => {
    if (!clubId) return
    setLoading(true)
    activitiesApi.list({ clubId }).then(setActivities).finally(() => setLoading(false))
  }

  useEffect(load, [clubId])

  if (loading) return <p className="page-loading">Loading activities…</p>

  const current = activities
    .filter(isCurrentActivity)
    .slice()
    .sort((a, b) => new Date(a.startsAtUtc).getTime() - new Date(b.startsAtUtc).getTime())
  const pendingConfirm = current.filter(needsAttendanceConfirm)
  const restCurrent = current.filter((activity) => !needsAttendanceConfirm(activity))
  const past = activities
    .filter(isPastActivity)
    .filter((activity, index, list) => list.findIndex((item) => item.id === activity.id) === index)
    .slice()
    .sort((a, b) => new Date(b.startsAtUtc).getTime() - new Date(a.startsAtUtc).getTime())

  return (
    <div>
      <h1 className="page-title">Activities</h1>
      <p className="page-subtitle">Club activities, races, and events</p>

      <div className="calendar-view-toggle" role="tablist" aria-label="Activity lists">
        <button
          type="button"
          role="tab"
          aria-selected={view === 'current'}
          className={`calendar-view-btn${view === 'current' ? ' active' : ''}`}
          onClick={() => setView('current')}
        >
          Current & Upcoming
        </button>
        <button
          type="button"
          role="tab"
          aria-selected={view === 'past'}
          className={`calendar-view-btn${view === 'past' ? ' active' : ''}`}
          onClick={() => setView('past')}
        >
          Past
        </button>
      </div>

      {view === 'current' ? (
        <>
          {pendingConfirm.length > 0 && (
            <section className="did-you-go-section">
              <h2 className="section-title">Did you go?</h2>
              {pendingConfirm.map((activity) => (
                <ActivityCard key={activity.id} activity={activity} onAttendanceUpdated={load} />
              ))}
            </section>
          )}

          {restCurrent.length === 0 && pendingConfirm.length === 0 ? (
            <div className="empty-state card">
              <p>No current or upcoming activities yet.</p>
            </div>
          ) : (
            restCurrent.map((activity) => (
              <ActivityCard key={activity.id} activity={activity} onAttendanceUpdated={load} />
            ))
          )}
        </>
      ) : past.length === 0 ? (
        <div className="empty-state card">
          <p>No past activities yet.</p>
        </div>
      ) : (
        past.map((activity) => (
          <ActivityCard key={activity.id} activity={activity} onAttendanceUpdated={load} />
        ))
      )}

      <div className="action-row">
        <Link to="/volunteer" className="btn btn-outline">
          View volunteer slots
        </Link>
      </div>
    </div>
  )
}
