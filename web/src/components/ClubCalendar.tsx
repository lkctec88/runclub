import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { profileApi } from '../api/services'
import type { CalendarItem } from '../types'
import { activityKindLabel } from '../types'
import { ActivityLocationLink } from './ActivityLocationLink'
import {
  addDays,
  addMonths,
  dayKey,
  formatMonthYear,
  formatWeekRange,
  getMonthGridDays,
  getViewRange,
  getWeekDays,
  sameDay,
  toApiDate,
  weekdayLabels,
  type CalendarView,
} from '../utils/calendar'

interface ClubCalendarProps {
  clubId: string
}

export function ClubCalendar({ clubId }: ClubCalendarProps) {
  const [view, setView] = useState<CalendarView>('week')
  const [cursor, setCursor] = useState(() => new Date())
  const [items, setItems] = useState<CalendarItem[]>([])
  const [loading, setLoading] = useState(true)
  const [selectedDay, setSelectedDay] = useState<Date | null>(null)

  const range = useMemo(() => getViewRange(view, cursor), [view, cursor])
  const today = useMemo(() => new Date(), [])

  useEffect(() => {
    setLoading(true)
    profileApi
      .calendar(clubId, { from: toApiDate(range.from), to: toApiDate(range.to) })
      .then(setItems)
      .finally(() => setLoading(false))
  }, [clubId, range.from, range.to])

  useEffect(() => {
    setSelectedDay(null)
  }, [view, cursor])

  const itemsByDay = useMemo(() => {
    const map = new Map<string, CalendarItem[]>()
    for (const item of items) {
      const key = dayKey(new Date(item.startsAtUtc))
      const list = map.get(key) ?? []
      list.push(item)
      map.set(key, list)
    }
    for (const list of map.values()) {
      list.sort((a, b) => new Date(a.startsAtUtc).getTime() - new Date(b.startsAtUtc).getTime())
    }
    return map
  }, [items])

  const navigate = (direction: -1 | 1) => {
    setCursor((current) =>
      view === 'week' ? addDays(current, direction * 7) : addMonths(current, direction),
    )
  }

  const goToday = () => setCursor(new Date())

  const title = view === 'week' ? formatWeekRange(cursor) : formatMonthYear(cursor)

  return (
    <div className="club-calendar">
      <div className="calendar-toolbar">
        <div className="calendar-nav">
          <button type="button" className="btn btn-ghost btn-sm calendar-nav-btn" onClick={() => navigate(-1)} aria-label="Previous">
            ‹
          </button>
          <button type="button" className="btn btn-ghost btn-sm calendar-nav-btn" onClick={() => navigate(1)} aria-label="Next">
            ›
          </button>
        </div>
        <h2 className="calendar-title">{title}</h2>
        <button type="button" className="btn btn-ghost btn-sm calendar-today-btn" onClick={goToday}>
          Today
        </button>
      </div>

      <div className="calendar-view-toggle" role="tablist" aria-label="Calendar view">
        <button
          type="button"
          role="tab"
          aria-selected={view === 'week'}
          className={`calendar-view-btn${view === 'week' ? ' active' : ''}`}
          onClick={() => setView('week')}
        >
          Week
        </button>
        <button
          type="button"
          role="tab"
          aria-selected={view === 'month'}
          className={`calendar-view-btn${view === 'month' ? ' active' : ''}`}
          onClick={() => setView('month')}
        >
          Month
        </button>
      </div>

      {loading ? (
        <p className="calendar-loading">Loading calendar…</p>
      ) : view === 'week' ? (
        <WeekView days={getWeekDays(cursor)} today={today} itemsByDay={itemsByDay} />
      ) : (
        <MonthView
          cursor={cursor}
          today={today}
          itemsByDay={itemsByDay}
          selectedDay={selectedDay}
          onSelectDay={setSelectedDay}
        />
      )}
    </div>
  )
}

function WeekView({
  days,
  today,
  itemsByDay,
}: {
  days: Date[]
  today: Date
  itemsByDay: Map<string, CalendarItem[]>
}) {
  return (
    <div className="calendar-week">
      {days.map((day) => {
        const key = dayKey(day)
        const dayItems = itemsByDay.get(key) ?? []
        const isToday = sameDay(day, today)
        return (
          <section key={key} className={`calendar-week-day${isToday ? ' is-today' : ''}`}>
            <header className="calendar-week-day-header">
              <span className="calendar-weekday">{day.toLocaleDateString(undefined, { weekday: 'short' })}</span>
              <span className="calendar-day-num">{day.getDate()}</span>
            </header>
            {dayItems.length === 0 ? (
              <p className="calendar-empty-day">No events</p>
            ) : (
              <ul className="calendar-event-list">
                {dayItems.map((item) => (
                  <CalendarEvent key={item.id} item={item} compact />
                ))}
              </ul>
            )}
          </section>
        )
      })}
    </div>
  )
}

function MonthView({
  cursor,
  today,
  itemsByDay,
  selectedDay,
  onSelectDay,
}: {
  cursor: Date
  today: Date
  itemsByDay: Map<string, CalendarItem[]>
  selectedDay: Date | null
  onSelectDay: (day: Date | null) => void
}) {
  const gridDays = getMonthGridDays(cursor)
  const currentMonth = cursor.getMonth()
  const selectedItems = selectedDay ? (itemsByDay.get(dayKey(selectedDay)) ?? []) : []

  return (
    <>
      <div className="calendar-month-grid">
        {weekdayLabels().map((label) => (
          <div key={label} className="calendar-month-weekday">
            {label}
          </div>
        ))}
        {gridDays.map((day) => {
          const key = dayKey(day)
          const dayItems = itemsByDay.get(key) ?? []
          const isToday = sameDay(day, today)
          const isCurrentMonth = day.getMonth() === currentMonth
          const isSelected = selectedDay ? sameDay(day, selectedDay) : false
          return (
            <button
              key={key}
              type="button"
              className={`calendar-month-cell${isCurrentMonth ? '' : ' is-other-month'}${isToday ? ' is-today' : ''}${isSelected ? ' is-selected' : ''}`}
              onClick={() => onSelectDay(isSelected ? null : day)}
            >
              <span className="calendar-month-day-num">{day.getDate()}</span>
              {dayItems.length > 0 && (
                <span className="calendar-month-dots" aria-hidden="true">
                  {dayItems.slice(0, 3).map((item) => (
                    <span
                      key={item.id}
                      className={`calendar-dot${item.isTrainingSession ? ' calendar-dot--training' : ''}`}
                    />
                  ))}
                </span>
              )}
            </button>
          )
        })}
      </div>

      {selectedDay && (
        <section className="calendar-day-detail card">
          <h3 className="calendar-day-detail-title">
            {selectedDay.toLocaleDateString(undefined, { weekday: 'long', month: 'long', day: 'numeric' })}
          </h3>
          {selectedItems.length === 0 ? (
            <p className="calendar-empty-day">No events on this day.</p>
          ) : (
            <ul className="calendar-event-list">
              {selectedItems.map((item) => (
                <CalendarEvent key={item.id} item={item} />
              ))}
            </ul>
          )}
        </section>
      )}
    </>
  )
}

function CalendarEvent({ item, compact = false }: { item: CalendarItem; compact?: boolean }) {
  const date = new Date(item.startsAtUtc)
  const time = date.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })

  return (
    <li className={`calendar-event${compact ? ' calendar-event--compact' : ''}`}>
      <Link to={`/activities/${item.id}`} className="calendar-event-link">
        <span className="calendar-event-time">{time}</span>
        <span className="calendar-event-body">
          <strong>{item.title}</strong>
          {!compact && (
            <span className="calendar-event-meta">
              {activityKindLabel(item.kind)}
              {item.isTrainingSession ? ' · Training' : ''}
            </span>
          )}
        </span>
      </Link>
      <ActivityLocationLink
        location={item.location}
        meetingPoint={item.meetingPoint}
        className="calendar-event-location"
      />
    </li>
  )
}
