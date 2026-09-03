import { ClubCalendar } from '../components/ClubCalendar'
import { LinkToCalendar } from '../components/LinkToCalendar'
import { useAuth } from '../auth/AuthContext'

export function CalendarPage() {
  const { clubId } = useAuth()

  if (!clubId) return <p className="page-loading">Loading calendar…</p>

  return (
    <div>
      <h1 className="page-title">Calendar</h1>
      <p className="page-subtitle">Everything happening at your club</p>
      <LinkToCalendar />
      <ClubCalendar clubId={clubId} />
    </div>
  )
}
