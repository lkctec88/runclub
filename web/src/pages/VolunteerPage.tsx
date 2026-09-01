import { useEffect, useState } from 'react'
import { activitiesApi } from '../api/services'
import { ActivityCard } from '../components/ActivityCard'
import { canHaveVolunteerSlots } from '../components/VolunteerRoles'
import { useAuth } from '../auth/AuthContext'
import type { ActivitySummary } from '../types'

export function VolunteerPage() {
  const { clubId } = useAuth()
  const [activities, setRuns] = useState<ActivitySummary[]>([])
  const [loading, setLoading] = useState(true)

  const load = async () => {
    if (!clubId) return
    setLoading(true)
    try {
      const all = await activitiesApi.list({ clubId })
      setRuns(
        all.filter(
          (activity) => canHaveVolunteerSlots(activity.kind) && (activity.volunteerSlots?.length ?? 0) > 0,
        ),
      )
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void load()
  }, [clubId])

  if (!clubId || loading) return <p className="page-loading">Loading volunteer roles…</p>

  return (
    <div>
      <h1 className="page-title">Volunteer</h1>
      <p className="page-subtitle">Sign up for open roles at upcoming activities</p>
      {activities.length === 0 ? (
        <div className="empty-state card">No open volunteer roles right now.</div>
      ) : (
        activities.map((activity) => (
          <ActivityCard key={activity.id} activity={activity} onVolunteerUpdated={load} onAttendanceUpdated={load} />
        ))
      )}
    </div>
  )
}
