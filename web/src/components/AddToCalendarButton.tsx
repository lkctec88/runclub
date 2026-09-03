import { useEffect, useRef, useState } from 'react'
import { IconCalendarPlus } from '@tabler/icons-react'
import { activitiesApi } from '../api/services'
import { activityIcsHref, ApiError } from '../api/client'
import { AttendanceStatus } from '../types'
import { googleCalendarUrl, outlookCalendarUrl, type CalendarEventInput } from '../utils/calendarLinks'

export function AddToCalendarButton({
  event,
  onGoing,
  variant = 'link',
}: {
  event: CalendarEventInput
  onGoing?: () => void
  variant?: 'link' | 'button'
}) {
  const [open, setOpen] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const rootRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) return
    const onPointerDown = (pointerEvent: PointerEvent) => {
      if (!rootRef.current?.contains(pointerEvent.target as Node)) setOpen(false)
    }
    document.addEventListener('pointerdown', onPointerDown)
    return () => document.removeEventListener('pointerdown', onPointerDown)
  }, [open])

  const markGoingThen = async (openUrl: string, download = false) => {
    if (busy) return
    setBusy(true)
    setError('')
    try {
      await activitiesApi.setAttendance(event.id, AttendanceStatus.Going)
      onGoing?.()
      if (download) {
        const link = document.createElement('a')
        link.href = openUrl
        link.download = `${event.title}.ics`
        document.body.appendChild(link)
        link.click()
        link.remove()
      } else {
        window.open(openUrl, '_blank', 'noopener,noreferrer')
      }
      setOpen(false)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not add this activity')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div ref={rootRef} className="add-to-calendar" onClick={(e) => e.stopPropagation()}>
      <button
        type="button"
        className={variant === 'button' ? 'btn btn-outline' : 'activity-calendar-link'}
        aria-expanded={open}
        disabled={busy}
        onClick={() => setOpen((current) => !current)}
      >
        <IconCalendarPlus size={16} stroke={1.8} aria-hidden="true" />
        {busy ? 'Adding…' : 'Add to my calendar'}
      </button>
      {open && (
        <div className="add-to-calendar-menu" role="menu">
          <button type="button" role="menuitem" disabled={busy} onClick={() => void markGoingThen(googleCalendarUrl(event))}>
            Google Calendar
          </button>
          <button type="button" role="menuitem" disabled={busy} onClick={() => void markGoingThen(outlookCalendarUrl(event))}>
            Outlook
          </button>
          <button
            type="button"
            role="menuitem"
            disabled={busy}
            onClick={() => void markGoingThen(activityIcsHref(event.id), true)}
          >
            Apple / other
          </button>
        </div>
      )}
      {error && (
        <p className="form-error" role="alert">
          {error}
        </p>
      )}
    </div>
  )
}
