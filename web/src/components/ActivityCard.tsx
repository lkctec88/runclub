import { useNavigate } from 'react-router-dom'
import type { ActivitySummary } from '../types'
import { activityKindLabel } from '../types'
import { ActivityLocationLink } from './ActivityLocationLink'
import { ActivityTagList } from './ActivityTagList'
import { VolunteerRoles } from './VolunteerRoles'
import { DidYouGoPrompt } from './DidYouGoPrompt'
import { GoingRsvp } from './GoingRsvp'
import { GoingPeople } from './GoingPeople'

export function ActivityCard({
  activity,
  onVolunteerUpdated,
  onAttendanceUpdated,
}: {
  activity: ActivitySummary
  onVolunteerUpdated?: () => void
  onAttendanceUpdated?: () => void
}) {
  const navigate = useNavigate()
  const date = new Date(activity.startsAtUtc)

  return (
    <div
      className="card activity-card"
      role="link"
      tabIndex={0}
      onClick={(e) => {
        if ((e.target as HTMLElement).closest('button, a, .activity-volunteer-slots, .did-you-go, .activity-going-rsvp, .activity-going-count, .dialog-backdrop')) return
        navigate(`/activities/${activity.id}`)
      }}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault()
          navigate(`/activities/${activity.id}`)
        }
      }}
    >
      <div className="activity-card-header">
        <span className={`badge badge-${['clubrun', 'race', 'personalrun'][activity.kind]}`}>
          {activityKindLabel(activity.kind)}
        </span>
        {activity.isTrainingSession && <span className="badge badge-training">Training</span>}
        <ActivityTagList tags={activity.tags} />
      </div>
      <h3>{activity.title}</h3>
      <p className="activity-meta">
        {date.toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' })}
        {' · '}
        {date.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })}
      </p>
      {activity.distanceMiles && <p className="activity-meta">{activity.distanceMiles} miles · {activity.paceGroups ?? 'Mixed pace'}</p>}
      <ActivityLocationLink location={activity.location} meetingPoint={activity.meetingPoint} />
      <VolunteerRoles
        activityId={activity.id}
        runKind={activity.kind}
        slots={activity.volunteerSlots}
        compact
        confirmRelease={false}
        onUpdated={onVolunteerUpdated}
      />
      <DidYouGoPrompt activity={activity} onUpdated={onAttendanceUpdated} />
      <GoingRsvp activity={activity} onUpdated={onAttendanceUpdated} />
      <div className="activity-stats">
        {activity.goingCount !== undefined && (
          <GoingPeople
            activityId={activity.id}
            runTitle={activity.title}
            goingCount={activity.goingCount}
            people={activity.goingMembers}
          />
        )}
        {activity.availableSlots !== undefined && activity.availableSlots > 0 && (
          <span className="volunteer-open">{activity.availableSlots} volunteer roles open</span>
        )}
      </div>
    </div>
  )
}
