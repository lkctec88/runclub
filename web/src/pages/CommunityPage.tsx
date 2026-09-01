import { useEffect, useState } from 'react'
import { profileApi } from '../api/services'
import { useAuth } from '../auth/AuthContext'
import type { MemberProfile, TrainingGroup } from '../types'

export function CommunityPage() {
  const { clubId } = useAuth()
  const [profiles, setProfiles] = useState<MemberProfile[]>([])
  const [matches, setMatches] = useState<{ profile: MemberProfile; score: number }[]>([])
  const [groups, setGroups] = useState<TrainingGroup[]>([])

  useEffect(() => {
    if (!clubId) return
    profileApi.clubProfiles(clubId).then(setProfiles)
    profileApi.findRunners(clubId).then(setMatches)
    profileApi.trainingGroups(clubId).then(setGroups)
  }, [clubId])

  return (
    <div>
      <h1 className="page-title">Club</h1>
      <p className="page-subtitle">Members, groups & Find Your Runners</p>

      {matches.length > 0 && (
        <>
          <h2 style={{ fontSize: '1rem', color: 'var(--navy)' }}>Find Your Runners</h2>
          {matches.map((m) => (
            <div key={m.profile.id} className="card">
              <strong>
                {m.profile.firstName} {m.profile.lastName}
              </strong>
              {m.profile.typicalPace && <p className="activity-meta">Pace: {m.profile.typicalPace}</p>}
            </div>
          ))}
        </>
      )}

      <h2 style={{ fontSize: '1rem', color: 'var(--navy)', marginTop: '1rem' }}>Training groups</h2>
      {groups.length === 0 ? (
        <div className="empty-state card">No groups yet.</div>
      ) : (
        groups.map((g) => (
          <div key={g.id} className="card">
            <strong>{g.name}</strong>
            {g.targetTime && <p className="activity-meta">Target: {g.targetTime}</p>}
            <p className="activity-meta">{g.members.length} members</p>
          </div>
        ))
      )}

      <h2 style={{ fontSize: '1rem', color: 'var(--navy)', marginTop: '1rem' }}>Members ({profiles.length})</h2>
      {profiles.slice(0, 10).map((p) => (
        <div key={p.id} className="card">
          {p.firstName} {p.lastName}
          {p.typicalPace && <span className="activity-meta"> · {p.typicalPace}</span>}
        </div>
      ))}
    </div>
  )
}
