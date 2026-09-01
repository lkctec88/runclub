import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { activitiesApi } from '../api/services'
import { ActivityCard } from '../components/ActivityCard'
import { useAuth } from '../auth/AuthContext'
import type { ActivitySummary } from '../types'

export function TrainingPage() {
  const { clubId } = useAuth()
  const [sessions, setSessions] = useState<ActivitySummary[]>([])

  useEffect(() => {
    if (!clubId) return
    activitiesApi.list({ clubId, trainingOnly: true }).then(setSessions)
  }, [clubId])

  return (
    <div>
      <h1 className="page-title">Training</h1>
      <p className="page-subtitle">Structured sessions — in person or virtual</p>
      {sessions.length === 0 ? (
        <div className="empty-state card">No training sessions scheduled.</div>
      ) : (
        sessions.map((s) => <ActivityCard key={s.id} activity={s} />)
      )}
      <Link to="/profile" className="btn btn-outline" style={{ marginTop: '1rem' }}>
        Manage your goals
      </Link>
    </div>
  )
}
