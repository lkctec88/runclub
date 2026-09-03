import type { ActivityKind } from '../types'
import { activityEffectiveEnd, activityLocationLabel } from './activity'

export type CalendarEventInput = {
  id: string
  title: string
  kind: ActivityKind
  startsAtUtc: string
  endsAtUtc?: string
  location?: string
  meetingPoint?: string
  description?: string
}

function compactUtc(iso: string) {
  return new Date(iso).toISOString().replace(/[-:]/g, '').replace(/\.\d{3}/, '')
}

function isoUtc(iso: string) {
  return new Date(iso).toISOString().replace(/\.\d{3}/, '')
}

function eventTimes(event: CalendarEventInput) {
  const start = event.startsAtUtc
  const end = activityEffectiveEnd(event).toISOString()
  return { start, end }
}

function eventLocation(event: CalendarEventInput) {
  return activityLocationLabel(event.location, event.meetingPoint) ?? ''
}

export function googleCalendarUrl(event: CalendarEventInput) {
  const { start, end } = eventTimes(event)
  const params = new URLSearchParams({
    action: 'TEMPLATE',
    text: event.title,
    dates: `${compactUtc(start)}/${compactUtc(end)}`,
    details: event.description ?? '',
    location: eventLocation(event),
  })
  return `https://calendar.google.com/calendar/render?${params.toString()}`
}

export function outlookCalendarUrl(event: CalendarEventInput) {
  const { start, end } = eventTimes(event)
  const params = new URLSearchParams({
    path: '/calendar/action/compose',
    rru: 'addevent',
    subject: event.title,
    startdt: isoUtc(start),
    enddt: isoUtc(end),
    body: event.description ?? '',
    location: eventLocation(event),
  })
  return `https://outlook.live.com/calendar/0/deeplink/compose?${params.toString()}`
}
