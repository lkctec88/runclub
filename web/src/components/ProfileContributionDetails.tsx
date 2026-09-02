import { Link } from 'react-router-dom'
import { ActivityCard } from './ActivityCard'
import { ActivityLocationLink } from './ActivityLocationLink'
import type { ProfileContributions } from '../types'
import { contributionRunToSummary, activityKindLabel, volunteerSlotLabel } from '../types'
import { VolunteerSlotStatus } from '../types'

export type ProfileSection = 'activities' | 'volunteer' | 'led' | 'training'

interface ProfileContributionDetailsProps {
  section: ProfileSection
  data: ProfileContributions
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

export function ProfileContributionDetails({ section, data }: ProfileContributionDetailsProps) {
  if (section === 'activities') {
    const { activitiesSignedUp, activitiesCompleted } = data
    if (activitiesSignedUp.length === 0 && activitiesCompleted.length === 0) {
      return <p className="profile-detail-empty">No activities signed up or completed yet.</p>
    }
    return (
      <div className="profile-detail-panel">
        {activitiesSignedUp.length > 0 && (
          <>
            <h3 className="profile-detail-heading">Signed up for</h3>
            {activitiesSignedUp.map(({ activity, paceGroup }) => (
              <div key={activity.id} className="profile-detail-item">
                <ActivityCard activity={contributionRunToSummary(activity)} />
                {paceGroup && <p className="activity-meta profile-detail-note">Pace group: {paceGroup}</p>}
              </div>
            ))}
          </>
        )}
        {activitiesCompleted.length > 0 && (
          <>
            <h3 className="profile-detail-heading">Completed</h3>
            {activitiesCompleted.map(({ activity, confirmedAtUtc }) => (
              <Link key={`${activity.id}-${confirmedAtUtc}`} to={`/activities/${activity.id}`} className="card profile-detail-card">
                <strong>{activity.title}</strong>
                <p className="activity-meta">{formatDate(activity.startsAtUtc)}</p>
                <p className="activity-meta">Went · {formatDate(confirmedAtUtc)}</p>
                <ActivityLocationLink location={activity.location} meetingPoint={activity.meetingPoint} />
              </Link>
            ))}
          </>
        )}
      </div>
    )
  }

  if (section === 'volunteer') {
    if (data.volunteerShifts.length === 0) {
      return <p className="profile-detail-empty">No volunteer shifts yet.</p>
    }
    return (
      <div className="profile-detail-panel">
        {data.volunteerShifts.map((shift) => (
          <Link key={shift.id} to={`/activities/${shift.activity.id}`} className="card profile-detail-card">
            <div className="volunteer-role-header">
              <strong>{volunteerSlotLabel(shift)}</strong>
              <span className={`badge ${shift.status === VolunteerSlotStatus.Completed ? 'badge-training' : 'badge-race'}`}>
                {shift.status === VolunteerSlotStatus.Completed ? 'Completed' : 'Signed up'}
              </span>
            </div>
            <p className="activity-meta">{shift.activity.title}</p>
            <p className="activity-meta">{formatDate(shift.activity.startsAtUtc)} · {activityKindLabel(shift.activity.kind)}</p>
            {shift.description && <p className="activity-meta">{shift.description}</p>}
            <ActivityLocationLink location={shift.activity.location} meetingPoint={shift.activity.meetingPoint} />
          </Link>
        ))}
      </div>
    )
  }

  if (section === 'led') {
    if (data.activitiesLed.length === 0) {
      return <p className="profile-detail-empty">No activities led yet.</p>
    }
    return (
      <div className="profile-detail-panel">
        {data.activitiesLed.map(({ activity, role, source }) => (
          <Link key={`${activity.id}-${source}-${role ?? ''}`} to={`/activities/${activity.id}`} className="card profile-detail-card">
            <strong>{activity.title}</strong>
            <p className="activity-meta">
              {formatDate(activity.startsAtUtc)} · {role ?? 'Activity leader'}
            </p>
            <ActivityLocationLink location={activity.location} meetingPoint={activity.meetingPoint} />
          </Link>
        ))}
      </div>
    )
  }

  if (data.trainingSessions.length === 0) {
    return <p className="profile-detail-empty">No training sessions completed yet.</p>
  }

  return (
    <div className="profile-detail-panel">
      {data.trainingSessions.map(({ activity, mode, distanceMiles, timeMinutes, effort }) => (
        <Link key={activity.id} to={`/activities/${activity.id}`} className="card profile-detail-card">
          <strong>{activity.title}</strong>
          <p className="activity-meta">{formatDate(activity.startsAtUtc)}</p>
          <p className="activity-meta">
            {mode === 1 ? 'Virtual' : 'In person'}
            {distanceMiles ? ` · ${distanceMiles} miles` : ''}
            {timeMinutes ? ` · ${timeMinutes} min` : ''}
            {effort ? ` · ${effort}` : ''}
          </p>
          <ActivityLocationLink location={activity.location} meetingPoint={activity.meetingPoint} />
        </Link>
      ))}
    </div>
  )
}
