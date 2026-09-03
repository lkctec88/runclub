import { useEffect, useState } from 'react'
import { profileApi } from '../api/services'
import { LinkToCalendar } from '../components/LinkToCalendar'
import { useAuth } from '../auth/AuthContext'
import type { MemberProfile, ProfileContributions } from '../types'
import {
  ProfileContributionDetails,
  type ProfileSection,
} from '../components/ProfileContributionDetails'

export function ProfilePage() {
  const { user } = useAuth()
  const [profile, setProfile] = useState<MemberProfile | null>(user?.profile ?? null)
  const [contributions, setContributions] = useState<ProfileContributions | null>(null)
  const [openSection, setOpenSection] = useState<ProfileSection | null>(null)
  const [goalLabel, setGoalLabel] = useState('')

  useEffect(() => {
    profileApi.me().then(setProfile)
    profileApi.contributions().then(setContributions)
  }, [])

  const addGoal = async () => {
    if (!goalLabel.trim()) return
    await profileApi.addGoal({ label: goalLabel, isActive: true })
    setGoalLabel('')
    profileApi.me().then(setProfile)
  }

  const toggleSection = (section: ProfileSection) => {
    setOpenSection((current) => (current === section ? null : section))
  }

  const p = profile

  const statItems: { id: ProfileSection; value: number; label: string }[] = [
    {
      id: 'activities',
      value: (contributions?.activitiesSignedUp.length ?? 0) + (contributions?.activitiesCompleted.length ?? 0),
      label: 'Activities',
    },
    {
      id: 'volunteer',
      value: p?.volunteerShifts ?? 0,
      label: 'Volunteer shifts',
    },
    {
      id: 'led',
      value: p?.activitiesLed ?? 0,
      label: 'Activities led',
    },
    {
      id: 'training',
      value: p?.trainingSessionsCompleted ?? 0,
      label: 'Training done',
    },
  ]

  return (
    <div>
      <h1 className="page-title">Your profile</h1>
      {p && (
        <>
          <div className="card">
            <h2 style={{ margin: '0 0 0.5rem', color: 'var(--navy)' }}>
              {p.firstName} {p.lastName}
            </h2>
            {p.typicalPace && <p className="activity-meta">Typical pace: {p.typicalPace}</p>}
            {p.currentRace && <p className="activity-meta">Current race: {p.currentRace}</p>}
          </div>

          <h2 style={{ fontSize: '1rem', color: 'var(--navy)' }}>Club contribution</h2>
          <p className="page-subtitle" style={{ marginTop: '-0.5rem' }}>
            Tap a section for details
          </p>
          <div className="stat-grid">
            {statItems.map((item) => (
              <button
                key={item.id}
                type="button"
                className={`stat-box stat-box--button${openSection === item.id ? ' stat-box--active' : ''}`}
                onClick={() => toggleSection(item.id)}
                aria-expanded={openSection === item.id}
              >
                <strong>{item.value}</strong>
                <span>{item.label}</span>
              </button>
            ))}
          </div>

          {openSection && contributions && (
            <ProfileContributionDetails section={openSection} data={contributions} />
          )}

          <h2 style={{ fontSize: '1rem', color: 'var(--navy)', marginTop: '1rem' }}>Training goal</h2>
          {p.trainingGoals?.filter((g) => g.isActive).map((g) => (
            <div key={g.id} className="card">
              🎯 {g.label}
              {g.targetTime && ` · ${g.targetTime}`}
            </div>
          ))}
          <div className="form-group">
            <input
              placeholder="e.g. First marathon"
              value={goalLabel}
              onChange={(e) => setGoalLabel(e.target.value)}
            />
          </div>
          <button type="button" className="btn btn-secondary" onClick={addGoal}>
            Set active goal
          </button>

          <h2 style={{ fontSize: '1rem', color: 'var(--navy)', marginTop: '1.5rem' }}>Calendar sync</h2>
          <LinkToCalendar />
        </>
      )}
    </div>
  )
}
