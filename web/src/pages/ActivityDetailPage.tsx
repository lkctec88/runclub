import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { activitiesApi } from '../api/services'
import { ApiError } from '../api/client'
import type { ActivitySummary } from '../types'
import { hasDistinctMeetingPoint, isRsvpGoing, activityHasEnded } from '../utils/activity'
import { ActivityLocationLink } from '../components/ActivityLocationLink'
import { ActivityTagList } from '../components/ActivityTagList'
import { VolunteerRoles } from '../components/VolunteerRoles'
import { DidYouGoPrompt } from '../components/DidYouGoPrompt'
import { GoingRsvp } from '../components/GoingRsvp'
import { GoingPeople } from '../components/GoingPeople'

export function ActivityDetailPage() {
  const { id } = useParams<{ id: string }>()
  const [activity, setRun] = useState<ActivitySummary | null>(null)
  const [error, setError] = useState('')

  const load = () => {
    if (!id) return
    setError('')
    activitiesApi
      .get(id)
      .then(setRun)
      .catch((e) => {
        setRun(null)
        setError(e instanceof ApiError ? e.message : 'Could not load this activity')
      })
  }

  useEffect(load, [id])

  if (error) return <p className="form-error">{error}</p>
  if (!activity) return <p className="page-loading">Loading…</p>

  const date = new Date(activity.startsAtUtc)
  const showMeetingPoint = hasDistinctMeetingPoint(activity.location, activity.meetingPoint)
  const ended = activityHasEnded(activity)
  const going = isRsvpGoing(activity.myAttendance)

  return (
    <div>
      <h1 className="page-title">{activity.title}</h1>
      <div className="activity-card-header" style={{ marginBottom: '0.75rem' }}>
        <ActivityTagList tags={activity.tags} />
      </div>
      <div className="card">
        <p className="activity-meta">
          {date.toLocaleDateString(undefined, { weekday: 'long', month: 'long', day: 'numeric' })}
          {' · '}
          {date.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })}
        </p>
        <ActivityLocationLink
          location={activity.location}
          meetingPoint={activity.meetingPoint}
          variant="detail"
        />
        {showMeetingPoint && activity.meetingPoint && (
          <p className="activity-meta">Meet at {activity.meetingPoint}</p>
        )}
        {activity.distanceMiles && <p className="activity-meta">{activity.distanceMiles} miles · {activity.paceGroups}</p>}
        {activity.description && <p className="activity-meta">{activity.description}</p>}
        <div className="activity-stats">
          <GoingPeople
            activityId={activity.id}
            runTitle={activity.title}
            goingCount={activity.goingCount ?? 0}
            people={activity.goingMembers}
          />
        </div>
        {activity.isTrainingSession && activity.workoutInstructions && (
          <pre style={{ whiteSpace: 'pre-wrap', fontSize: '0.85rem', marginTop: '0.75rem' }}>
            {activity.workoutInstructions}
          </pre>
        )}
      </div>

      <VolunteerRoles
        activityId={activity.id}
        runKind={activity.kind}
        slots={activity.volunteerSlots}
        compact
        showHeading
        confirmRelease={false}
        onUpdated={load}
      />

      <DidYouGoPrompt activity={activity} onUpdated={load} />

      <div className="action-row">
        <GoingRsvp activity={activity} showJoin onUpdated={load} />
        {!ended && (
          <a href={`/api/activities/${activity.id}.ics`} className="btn btn-outline" download>
            Add to calendar
          </a>
        )}
        {ended && !going && <p className="activity-meta">This activity has ended.</p>}
      </div>
    </div>
  )
}
