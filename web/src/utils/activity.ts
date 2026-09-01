import { ActivityKind, AttendanceStatus, type ActivityAttendance, type ActivitySummary } from '../types'

export function activityLocationLabel(location?: string, meetingPoint?: string) {
  if (location && meetingPoint && location !== meetingPoint) {
    return `${location} · ${meetingPoint}`
  }
  return location ?? meetingPoint ?? null
}

export function hasDistinctMeetingPoint(location?: string, meetingPoint?: string) {
  return Boolean(location && meetingPoint && location !== meetingPoint)
}

const DEFAULT_RUN_DURATION_MS = 2 * 60 * 60 * 1000

export function activityEffectiveEnd(activity: { startsAtUtc: string; endsAtUtc?: string }) {
  if (activity.endsAtUtc) return new Date(activity.endsAtUtc)
  return new Date(new Date(activity.startsAtUtc).getTime() + DEFAULT_RUN_DURATION_MS)
}

export function activityHasEnded(activity: { startsAtUtc: string; endsAtUtc?: string }) {
  return Date.now() >= activityEffectiveEnd(activity).getTime()
}

export function isRsvpGoing(attendance?: ActivityAttendance | null) {
  if (!attendance) return false
  if (attendance.isGoing === true) return true
  if (attendance.isGoing === false) return false
  return (
    attendance.status === AttendanceStatus.Going ||
    attendance.status === 'Going' ||
    Number(attendance.status) === AttendanceStatus.Going
  )
}

export function needsAttendanceConfirm(activity: ActivitySummary) {
  if (!activityHasEnded(activity) || activity.kind === ActivityKind.PersonalActivity) return false
  if (activity.myAttendance?.attended == null) return isRsvpGoing(activity.myAttendance)
  return false
}

export const CURRENT_ACTIVITY_DAYS = 5

function startOfLocalDay(value: Date) {
  return new Date(value.getFullYear(), value.getMonth(), value.getDate()).getTime()
}

export function isPastActivity(activity: { startsAtUtc: string }) {
  return startOfLocalDay(new Date(activity.startsAtUtc)) < startOfLocalDay(new Date())
}

export function isCurrentActivity(activity: { startsAtUtc: string }) {
  const startDay = startOfLocalDay(new Date(activity.startsAtUtc))
  const today = new Date()
  const firstDay = startOfLocalDay(today)
  const lastDay = new Date(today.getFullYear(), today.getMonth(), today.getDate() + CURRENT_ACTIVITY_DAYS - 1).getTime()
  return startDay >= firstDay && startDay <= lastDay
}
