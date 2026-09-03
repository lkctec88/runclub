import { useEffect, useMemo, useState } from 'react'
import { activitiesApi } from '../api/services'
import { ActivityCard } from '../components/ActivityCard'
import { canHaveVolunteerSlots } from '../components/VolunteerRoles'
import { useAuth } from '../auth/AuthContext'
import type { ActivitySummary } from '../types'
import { isPastActivity, matchesActivitySearch } from '../utils/activity'

export function VolunteerPage() {
  const { clubId } = useAuth()
  const [activities, setRuns] = useState<ActivitySummary[]>([])
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)

  const load = async () => {
    if (!clubId) return
    setLoading(true)
    try {
      const all = await activitiesApi.list({ clubId })
      setRuns(
        all
          .filter(
            (activity) =>
              !isPastActivity(activity) &&
              canHaveVolunteerSlots(activity.kind) &&
              (activity.volunteerSlots?.length ?? 0) > 0,
          )
          .slice()
          .sort((a, b) => new Date(a.startsAtUtc).getTime() - new Date(b.startsAtUtc).getTime()),
      )
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void load()
  }, [clubId])

  const visible = useMemo(
    () => activities.filter((activity) => matchesActivitySearch(search, activity)),
    [activities, search],
  )
  const searching = search.trim().length > 0

  if (!clubId || loading) return <p className="page-loading">Loading volunteer roles…</p>

  return (
    <div>
      <h1 className="page-title">Volunteer</h1>
      <p className="page-subtitle">Sign up for open roles at upcoming activities</p>
      <div className="form-group">
        <label htmlFor="volunteer-search">Search</label>
        <input
          id="volunteer-search"
          type="search"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="e.g. marshal, trail, Tuesday"
          autoComplete="off"
        />
      </div>
      {searching && (
        <p className="activity-meta" style={{ marginBottom: '0.75rem' }}>
          {visible.length} match{visible.length === 1 ? '' : 'es'}
        </p>
      )}
      {visible.length === 0 ? (
        <div className="empty-state card">
          {searching ? 'No volunteer roles match that search.' : 'No open volunteer roles right now.'}
        </div>
      ) : (
        visible.map((activity) => (
          <ActivityCard key={activity.id} activity={activity} onVolunteerUpdated={load} onAttendanceUpdated={load} />
        ))
      )}
    </div>
  )
}
