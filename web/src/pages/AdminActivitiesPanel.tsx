import { useEffect, useMemo, useState } from 'react'
import { IconChevronLeft } from '@tabler/icons-react'
import { activitiesApi, clubsApi, volunteerApi } from '../api/services'
import { ApiError } from '../api/client'
import {
  ActivityKind,
  RecurrenceFrequency,
  TrainingSessionType,
  activityKindLabel,
  volunteerSlotLabel,
  type ActivitySummary,
  type VolunteerRoleType,
  type VolunteerSlot,
} from '../types'
import { isPastActivity, matchesActivitySearch } from '../utils/activity'
import { ActivityTagList } from '../components/ActivityTagList'

type ActivitySection = 'new' | 'existing'
type FormKind = 'clubrun' | 'race' | 'training'

const CREATE_TYPE = '__create__'

const sessionTypes: { value: TrainingSessionType; label: string }[] = [
  { value: TrainingSessionType.Hills, label: 'Hills' },
  { value: TrainingSessionType.TrackIntervals, label: 'Track intervals' },
  { value: TrainingSessionType.Tempo, label: 'Tempo' },
  { value: TrainingSessionType.Fartlek, label: 'Fartlek' },
  { value: TrainingSessionType.SpeedWork, label: 'Speed work' },
  { value: TrainingSessionType.Other, label: 'Other' },
]

type NeedRow = {
  key: string
  typeId: string
  newTypeName: string
  tag: string
  count: string
}

function newNeedRow(createType: boolean): NeedRow {
  return {
    key: crypto.randomUUID(),
    typeId: createType ? CREATE_TYPE : '',
    newTypeName: '',
    tag: '',
    count: '1',
  }
}

const START_HOURS = Array.from({ length: 24 }, (_, hour) => String(hour).padStart(2, '0'))
const START_MINUTES = ['00', '15', '30', '45'] as const

function pad2(value: number) {
  return String(value).padStart(2, '0')
}

function splitLocalDateTime(iso: string) {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return { date: '', hour: '19', minute: '00' }
  let hour = date.getHours()
  let minute = Math.round(date.getMinutes() / 15) * 15
  if (minute === 60) {
    minute = 0
    hour = (hour + 1) % 24
  }
  return {
    date: `${date.getFullYear()}-${pad2(date.getMonth() + 1)}-${pad2(date.getDate())}`,
    hour: pad2(hour),
    minute: pad2(minute),
  }
}

function combineLocalDateTime(date: string, hour: string, minute: string) {
  if (!date) return ''
  return new Date(`${date}T${hour}:${minute}:00`).toISOString()
}

function formKindFromActivity(activity: ActivitySummary): FormKind {
  if (activity.kind === ActivityKind.Race) return 'race'
  if (activity.isTrainingSession) return 'training'
  return 'clubrun'
}

function formatWhen(iso: string) {
  return new Date(iso).toLocaleString(undefined, {
    weekday: 'short',
    day: 'numeric',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
  })
}

export function AdminActivitiesPanel({ clubId }: { clubId: string }) {
  const [section, setSection] = useState<ActivitySection>('existing')
  const [editing, setEditing] = useState<ActivitySummary | null>(null)

  return (
    <div>
      <div className="slide-switch">
        <button
          type="button"
          className={`slide-switch-label${section === 'new' ? ' is-active' : ''}`}
          aria-pressed={section === 'new'}
          onClick={() => {
            setEditing(null)
            setSection('new')
          }}
        >
          New
        </button>
        <button
          type="button"
          className={`slide-switch-track${section === 'existing' ? ' is-right' : ''}`}
          role="switch"
          aria-checked={section === 'existing'}
          aria-label="Existing activities"
          onClick={() => {
            setEditing(null)
            setSection(section === 'existing' ? 'new' : 'existing')
          }}
        >
          <span className="slide-switch-knob" aria-hidden="true" />
        </button>
        <button
          type="button"
          className={`slide-switch-label${section === 'existing' ? ' is-active' : ''}`}
          aria-pressed={section === 'existing'}
          onClick={() => {
            setEditing(null)
            setSection('existing')
          }}
        >
          Existing
        </button>
      </div>

      {section === 'new' && (
        <ActivityForm
          clubId={clubId}
          onCreated={() => {
            setEditing(null)
            setSection('existing')
          }}
        />
      )}
      {section === 'existing' && !(editing && !isPastActivity(editing)) && (
        <ExistingActivitiesList
          clubId={clubId}
          onEdit={(activity) => {
            if (!isPastActivity(activity)) setEditing(activity)
          }}
        />
      )}
      {section === 'existing' && editing && !isPastActivity(editing) && (
        <ActivityForm
          clubId={clubId}
          existing={editing}
          onCancel={() => setEditing(null)}
          onSaved={() => setEditing(null)}
        />
      )}
    </div>
  )
}

function ExistingActivitiesList({
  clubId,
  onEdit,
}: {
  clubId: string
  onEdit: (activity: ActivitySummary) => void
}) {
  const [activities, setActivities] = useState<ActivitySummary[]>([])
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const load = () => {
    setLoading(true)
    setError('')
    activitiesApi
      .list({ clubId })
      .then(setActivities)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Could not load activities'))
      .finally(() => setLoading(false))
  }

  useEffect(() => {
    load()
  }, [clubId])

  const upcoming = useMemo(
    () =>
      activities
        .filter((a) => !isPastActivity(a))
        .filter((activity) => matchesActivitySearch(search, activity))
        .slice()
        .sort((a, b) => new Date(a.startsAtUtc).getTime() - new Date(b.startsAtUtc).getTime()),
    [activities, search],
  )
  const searching = search.trim().length > 0

  if (loading) return <p className="page-loading">Loading activities…</p>

  return (
    <div className="admin-panel">
      {error && <p className="form-error">{error}</p>}
      <div className="card">
        <h2 className="admin-card-title">Find an activity</h2>
        <p className="activity-meta">Search by title, location, tag, or type.</p>
        <div className="form-group" style={{ marginBottom: 0 }}>
          <label htmlFor="sa-activity-search">Search</label>
          <input
            id="sa-activity-search"
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="e.g. Black Rocks, trail, race"
            autoComplete="off"
          />
        </div>
        {searching && (
          <p className="activity-meta" style={{ marginTop: '0.65rem' }}>
            {upcoming.length} match{upcoming.length === 1 ? '' : 'es'}
          </p>
        )}
      </div>
      <ActivityGroup
        items={upcoming}
        empty={searching ? 'No activities match that search.' : 'No upcoming activities.'}
        onEdit={onEdit}
      />
    </div>
  )
}

function ActivityGroup({
  items,
  empty,
  onEdit,
}: {
  items: ActivitySummary[]
  empty: string
  onEdit?: (activity: ActivitySummary) => void
}) {
  return (
    <section className="admin-activity-group">
      {items.length === 0 ? (
        <div className="empty-state card">{empty}</div>
      ) : (
        items.map((activity) => (
          <div key={activity.id} className="card admin-activity-row">
            <div>
              <div className="volunteer-role-header">
                <strong>{activity.title}</strong>
                <span className={`badge badge-${['clubrun', 'race', 'personalrun'][activity.kind]}`}>
                  {activityKindLabel(activity.kind)}
                </span>
              </div>
              <p className="activity-meta">{formatWhen(activity.startsAtUtc)}</p>
              {(activity.location || activity.meetingPoint) && (
                <p className="activity-meta">{activity.location ?? activity.meetingPoint}</p>
              )}
              <ActivityTagList tags={activity.tags} />
              {(activity.volunteerSlots?.length ?? 0) > 0 && (
                <ul className="activity-tags admin-activity-volunteers">
                  {(activity.volunteerSlots ?? []).map((slot) => (
                    <li key={slot.id} className="badge badge-volunteer">
                      {slot.assignedUserName
                        ? `${volunteerSlotLabel(slot)} · ${slot.assignedUserName}`
                        : volunteerSlotLabel(slot)}
                    </li>
                  ))}
                </ul>
              )}
            </div>
            {onEdit && (
              <button type="button" className="btn btn-outline btn-sm" onClick={() => onEdit(activity)}>
                Edit
              </button>
            )}
          </div>
        ))
      )}
    </section>
  )
}

function ActivityForm({
  clubId,
  existing,
  onCreated,
  onSaved,
  onCancel,
}: {
  clubId: string
  existing?: ActivitySummary
  onCreated?: () => void
  onSaved?: () => void
  onCancel?: () => void
}) {
  const [activityType, setActivityType] = useState<FormKind>(existing ? formKindFromActivity(existing) : 'clubrun')
  const [title, setTitle] = useState(existing?.title ?? '')
  const [description, setDescription] = useState(existing?.description ?? '')
  const initialWhen = existing ? splitLocalDateTime(existing.startsAtUtc) : { date: '', hour: '19', minute: '00' }
  const [startDate, setStartDate] = useState(initialWhen.date)
  const [startHour, setStartHour] = useState(initialWhen.hour)
  const [startMinute, setStartMinute] = useState(initialWhen.minute)
  const [location, setLocation] = useState(existing?.location ?? '')
  const [meetingPoint, setMeetingPoint] = useState(existing?.meetingPoint ?? '')
  const [distanceMiles, setDistanceMiles] = useState(existing?.distanceMiles ?? '')
  const [paceGroups, setPaceGroups] = useState(existing?.paceGroups ?? '')
  const [repeat, setRepeat] = useState<'none' | 'weekly' | 'monthly'>('none')
  const [repeatUntil, setRepeatUntil] = useState('')
  const [tags, setTags] = useState<string[]>(existing?.tags ?? [])
  const [tagDraft, setTagDraft] = useState('')
  const canRepeat = !existing && (activityType === 'clubrun' || activityType === 'training')
  const [sessionType, setSessionType] = useState<TrainingSessionType>(
    (existing?.sessionType as TrainingSessionType | undefined) ?? TrainingSessionType.Hills,
  )
  const [workoutInstructions, setWorkoutInstructions] = useState(existing?.workoutInstructions ?? '')
  const [targetPaceOrEffort, setTargetPaceOrEffort] = useState(existing?.targetPaceOrEffort ?? '')
  const [virtualParticipationEnabled, setVirtualParticipationEnabled] = useState(
    existing?.virtualParticipationEnabled ?? true,
  )
  const [types, setTypes] = useState<VolunteerRoleType[]>([])
  const [addedNeeds, setAddedNeeds] = useState<NeedRow[]>([])
  const [draft, setDraft] = useState<NeedRow>(() => newNeedRow(false))
  const [existingSlots, setExistingSlots] = useState<VolunteerSlot[]>(existing?.volunteerSlots ?? [])
  const [removedSlotIds, setRemovedSlotIds] = useState<string[]>([])
  const [saving, setSaving] = useState(false)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')

  const loadTypes = async () => {
    try {
      const rows = await clubsApi.volunteerRoleTypes(clubId)
      setTypes(rows)
      setDraft((current) => ({
        ...current,
        typeId: current.typeId || (rows.length === 0 ? CREATE_TYPE : current.typeId),
      }))
    } catch {
      setTypes([])
      setDraft((current) => ({ ...current, typeId: CREATE_TYPE }))
    }
  }

  useEffect(() => {
    void loadTypes()
    if (existing) {
      activitiesApi.get(existing.id).then((full) => {
        setExistingSlots(full.volunteerSlots ?? [])
        setTags(full.tags ?? [])
      }).catch(() => undefined)
    }
  }, [clubId, existing?.id])

  const resolvedTags = () => {
    const draftLabel = tagDraft.trim().slice(0, 40)
    if (!draftLabel) return tags
    if (tags.some((tag) => tag.toLowerCase() === draftLabel.toLowerCase())) return tags
    if (tags.length >= 12) return tags
    return [...tags, draftLabel]
  }

  const payload = (volunteerNeeds: { volunteerRoleTypeId: string; count: number; tag: string | null }[]) => {
    const isTraining = activityType === 'training'
    return {
      clubId,
      kind: activityType === 'race' ? ActivityKind.Race : ActivityKind.ClubActivity,
      title,
      description: description || null,
      startsAtUtc: combineLocalDateTime(startDate, startHour, startMinute),
      location: location || null,
      meetingPoint: meetingPoint || null,
      distanceMiles: distanceMiles.trim() || null,
      paceGroups: paceGroups || null,
      isTrainingSession: isTraining,
      sessionType: isTraining ? sessionType : null,
      workoutInstructions: isTraining ? workoutInstructions || null : null,
      targetPaceOrEffort: isTraining ? targetPaceOrEffort || null : null,
      virtualParticipationEnabled: isTraining && virtualParticipationEnabled,
      tags: resolvedTags(),
      volunteerNeeds,
      recurrenceFrequency:
        canRepeat && repeat === 'weekly'
          ? RecurrenceFrequency.Weekly
          : canRepeat && repeat === 'monthly'
            ? RecurrenceFrequency.Monthly
            : RecurrenceFrequency.None,
      recurrenceUntilUtc:
        canRepeat && repeat !== 'none' && repeatUntil
          ? new Date(`${repeatUntil}T23:59:59`).toISOString()
          : null,
    }
  }

  const typeName = (row: NeedRow) =>
    types.find((t) => t.id === row.typeId)?.name ?? row.newTypeName.trim()

  const addTag = () => {
    const label = tagDraft.trim().slice(0, 40)
    if (!label) return
    if (tags.some((tag) => tag.toLowerCase() === label.toLowerCase())) {
      setError('That tag is already on this activity.')
      return
    }
    if (tags.length >= 12) {
      setError('You can add up to 12 tags.')
      return
    }
    setError('')
    setTags((current) => [...current, label])
    setTagDraft('')
  }

  const resolveRow = async (row: NeedRow) => {
    let typeId = row.typeId
    const newName = row.newTypeName.trim()
    if (typeId === CREATE_TYPE || (!typeId && newName)) {
      if (!newName) return null
      const created = await clubsApi.createVolunteerRoleType(clubId, { name: newName })
      setTypes((current) =>
        [...current.filter((t) => t.id !== created.id), created].sort((a, b) => a.name.localeCompare(b.name)),
      )
      typeId = created.id
    }
    if (!typeId || typeId === CREATE_TYPE) return null
    return {
      volunteerRoleTypeId: typeId,
      count: Math.min(20, Math.max(1, Number(row.count) || 1)),
      tag: row.tag.trim() || null,
    }
  }

  const addVolunteer = async () => {
    setError('')
    try {
      const resolved = await resolveRow(draft)
      if (!resolved) {
        setError('Choose a volunteer type, or create one, before adding.')
        return
      }
      setAddedNeeds((current) => [
        ...current,
        {
          ...draft,
          key: crypto.randomUUID(),
          typeId: resolved.volunteerRoleTypeId,
          newTypeName: '',
          count: String(resolved.count),
          tag: resolved.tag ?? '',
        },
      ])
      setDraft(newNeedRow(false))
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not add volunteer')
    }
  }

  const removeExistingSlot = (slot: VolunteerSlot) => {
    if (slot.assignedUserId && !confirm(`Someone is already signed up as ${volunteerSlotLabel(slot)}. Remove this volunteer slot anyway?`)) {
      return
    }
    setExistingSlots((current) => current.filter((item) => item.id !== slot.id))
    setRemovedSlotIds((current) => (current.includes(slot.id) ? current : [...current, slot.id]))
  }

  const resolveNeeds = async () => {
    if (activityType === 'training') return []
    const resolved = []
    for (const row of addedNeeds) {
      const item = await resolveRow(row)
      if (item) resolved.push(item)
    }
    const draftItem = await resolveRow(draft)
    if (draftItem) resolved.push(draftItem)
    return resolved
  }

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setSaving(true)
    setError('')
    setMessage('')
    try {
      const volunteerNeeds = await resolveNeeds()
      if (existing) {
        await activitiesApi.update(existing.id, payload(volunteerNeeds))
        await Promise.all(removedSlotIds.map((slotId) => volunteerApi.remove(existing.id, slotId)))
        setMessage(`${title} saved.`)
        onSaved?.()
      } else {
        await activitiesApi.create(payload(volunteerNeeds))
        setMessage(`${title} created.`)
        onCreated?.()
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : existing ? 'Could not save activity' : 'Could not create activity')
    } finally {
      setSaving(false)
    }
  }

  const removeActivity = async () => {
    if (!existing) return
    if (!confirm(`Remove ${existing.title}?`)) return
    setSaving(true)
    setError('')
    try {
      await activitiesApi.remove(existing.id)
      onSaved?.()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not remove activity')
      setSaving(false)
    }
  }

  return (
    <form className="card admin-panel" onSubmit={submit}>
      {onCancel ? (
        <div className="admin-edit-bar">
          <button type="button" className="admin-edit-back" onClick={onCancel} aria-label="Back">
            <IconChevronLeft size={24} stroke={2} aria-hidden="true" />
          </button>
          <h2 className="admin-edit-title">Edit Activity</h2>
        </div>
      ) : (
        <h2 className="admin-card-title">Create activity</h2>
      )}
      <div className="form-group">
        <label htmlFor="sa-type">Type</label>
        <select
          id="sa-type"
          value={activityType}
          onChange={(e) => {
            const next = e.target.value as FormKind
            setActivityType(next)
            if (next === 'race') {
              setRepeat('none')
              setRepeatUntil('')
            }
          }}
        >
          <option value="clubrun">Club activity / event</option>
          <option value="race">Race</option>
          <option value="training">Training session</option>
        </select>
      </div>
      <div className="form-group">
        <label htmlFor="sa-title">Title</label>
        <input id="sa-title" value={title} onChange={(e) => setTitle(e.target.value)} required />
      </div>
      <div className="form-group">
        <span className="admin-card-title" style={{ display: 'block' }}>
          Tags
        </span>
        {tags.length > 0 && (
          <ul className="activity-tags" style={{ marginBottom: '0.65rem' }}>
            {tags.map((tag) => (
              <li key={tag} className="activity-tag">
                {tag}
                <button
                  type="button"
                  className="activity-tag-remove"
                  aria-label={`Remove tag ${tag}`}
                  onClick={() => setTags((current) => current.filter((item) => item !== tag))}
                >
                  ×
                </button>
              </li>
            ))}
          </ul>
        )}
        <div className="activity-tag-composer">
          <div className="form-group">
            <label htmlFor="sa-tag">Add a tag</label>
            <input
              id="sa-tag"
              value={tagDraft}
              maxLength={40}
              onChange={(e) => setTagDraft(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === 'Enter') {
                  e.preventDefault()
                  addTag()
                }
              }}
              placeholder="e.g. trail, beginner, social"
            />
          </div>
          <button type="button" className="btn btn-outline" onClick={addTag}>
            Add tag
          </button>
        </div>
      </div>
      <div className="form-group">
        <label htmlFor="sa-date">Date and time</label>
        <div className="form-datetime">
          <input
            id="sa-date"
            type="date"
            value={startDate}
            onChange={(e) => setStartDate(e.target.value)}
            required
          />
          <select
            id="sa-hour"
            aria-label="Hour"
            value={startHour}
            onChange={(e) => setStartHour(e.target.value)}
          >
            {START_HOURS.map((hour) => (
              <option key={hour} value={hour}>
                {hour}
              </option>
            ))}
          </select>
          <span className="form-datetime-sep" aria-hidden="true">
            :
          </span>
          <select
            id="sa-minute"
            aria-label="Minutes"
            value={startMinute}
            onChange={(e) => setStartMinute(e.target.value)}
          >
            {START_MINUTES.map((minute) => (
              <option key={minute} value={minute}>
                {minute}
              </option>
            ))}
          </select>
        </div>
      </div>
      {canRepeat && (
        <>
          <div className="form-group">
            <label htmlFor="sa-repeat">Repeat</label>
            <select
              id="sa-repeat"
              value={repeat}
              onChange={(e) => setRepeat(e.target.value as 'none' | 'weekly' | 'monthly')}
            >
              <option value="none">Does not repeat</option>
              <option value="weekly">Every week</option>
              <option value="monthly">Every month</option>
            </select>
          </div>
          {repeat !== 'none' && (
            <div className="form-group">
              <label htmlFor="sa-repeat-until">End date (optional)</label>
              <input
                id="sa-repeat-until"
                type="date"
                value={repeatUntil}
                min={startDate || undefined}
                onChange={(e) => setRepeatUntil(e.target.value)}
              />
              <p className="activity-meta">
                {repeatUntil
                  ? 'Creates one activity on each repeat up to this date.'
                  : 'No end date creates 12 activities (weekly or monthly).'}
              </p>
            </div>
          )}
        </>
      )}
      <div className="form-group">
        <label htmlFor="sa-location">Location</label>
        <input id="sa-location" value={location} onChange={(e) => setLocation(e.target.value)} />
      </div>
      <div className="form-group">
        <label htmlFor="sa-meet">Meeting point</label>
        <input id="sa-meet" value={meetingPoint} onChange={(e) => setMeetingPoint(e.target.value)} />
      </div>
      <div className="form-group">
        <label htmlFor="sa-distance">Distance (miles)</label>
        <input
          id="sa-distance"
          value={distanceMiles}
          onChange={(e) => setDistanceMiles(e.target.value)}
          maxLength={40}
          placeholder="e.g. 5 or 4-8"
        />
      </div>
      {activityType !== 'training' && (
        <div className="form-group">
          <label htmlFor="sa-pace">Pace groups</label>
          <input id="sa-pace" value={paceGroups} onChange={(e) => setPaceGroups(e.target.value)} placeholder="Mixed" />
        </div>
      )}
      {activityType === 'training' && (
        <>
          <div className="form-group">
            <label htmlFor="sa-session">Session type</label>
            <select
              id="sa-session"
              value={sessionType}
              onChange={(e) => setSessionType(Number(e.target.value) as TrainingSessionType)}
            >
              {sessionTypes.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </div>
          <div className="form-group">
            <label htmlFor="sa-workout">Workout instructions</label>
            <textarea
              id="sa-workout"
              rows={4}
              value={workoutInstructions}
              onChange={(e) => setWorkoutInstructions(e.target.value)}
            />
          </div>
          <div className="form-group">
            <label htmlFor="sa-effort">Target pace or effort</label>
            <input id="sa-effort" value={targetPaceOrEffort} onChange={(e) => setTargetPaceOrEffort(e.target.value)} />
          </div>
          <label className="admin-check">
            <input
              type="checkbox"
              checked={virtualParticipationEnabled}
              onChange={(e) => setVirtualParticipationEnabled(e.target.checked)}
            />
            Allow virtual participation
          </label>
        </>
      )}
      <div className="form-group">
        <label htmlFor="sa-desc">Description</label>
        <textarea id="sa-desc" rows={3} value={description} onChange={(e) => setDescription(e.target.value)} />
      </div>

      {activityType !== 'training' && (
        <div className="form-group">
          <span className="admin-card-title" style={{ display: 'block' }}>
            Volunteers
          </span>
          {(existingSlots.length > 0 || addedNeeds.length > 0) && (
            <ul className="volunteer-added-list">
              {existingSlots.map((slot) => (
                <li key={slot.id} className="volunteer-added-item">
                  <span>
                    {slot.assignedUserName
                      ? `${volunteerSlotLabel(slot)} · ${slot.assignedUserName}`
                      : volunteerSlotLabel(slot)}
                  </span>
                  <button type="button" className="volunteer-remove" onClick={() => removeExistingSlot(slot)}>
                    Remove
                  </button>
                </li>
              ))}
              {addedNeeds.map((row) => (
                <li key={row.key} className="volunteer-added-item">
                  <span>
                    {typeName(row)}
                    {row.tag.trim() ? ` · ${row.tag.trim()}` : ''}
                    {Number(row.count) > 1 ? ` × ${row.count}` : ''}
                  </span>
                  <button
                    type="button"
                    className="volunteer-remove"
                    onClick={() => setAddedNeeds((current) => current.filter((item) => item.key !== row.key))}
                  >
                    Remove
                  </button>
                </li>
              ))}
            </ul>
          )}

          <div className="volunteer-composer">
            <div className="form-group">
              <label htmlFor="vol-type">Type</label>
              <select
                id="vol-type"
                value={draft.typeId}
                onChange={(e) => setDraft((current) => ({ ...current, typeId: e.target.value, newTypeName: '' }))}
              >
                <option value="">{types.length === 0 ? 'No types yet' : 'Select type'}</option>
                {types.map((type) => (
                  <option key={type.id} value={type.id}>
                    {type.name}
                  </option>
                ))}
                <option value={CREATE_TYPE}>Create new type…</option>
              </select>
            </div>
            {(draft.typeId === CREATE_TYPE || types.length === 0) && (
              <div className="form-group">
                <label htmlFor="vol-new">New type name</label>
                <input
                  id="vol-new"
                  value={draft.newTypeName}
                  onChange={(e) =>
                    setDraft((current) => ({ ...current, newTypeName: e.target.value, typeId: CREATE_TYPE }))
                  }
                  placeholder="e.g. Marshal, Run lead"
                />
              </div>
            )}
            <div className="form-group">
              <label htmlFor="vol-tag">Tag (optional)</label>
              <input
                id="vol-tag"
                value={draft.tag}
                onChange={(e) => setDraft((current) => ({ ...current, tag: e.target.value }))}
                placeholder="e.g. marshall point 4, 8:30min/mi"
              />
            </div>
            <div className="form-group volunteer-need-count">
              <label htmlFor="vol-count">Needed</label>
              <input
                id="vol-count"
                type="number"
                min="1"
                max="20"
                value={draft.count}
                onChange={(e) => setDraft((current) => ({ ...current, count: e.target.value }))}
              />
            </div>
            <button type="button" className="btn btn-outline" onClick={() => void addVolunteer()}>
              Add volunteer
            </button>
          </div>
        </div>
      )}

      {error && <p className="form-error">{error}</p>}
      {message && <p className="volunteer-message">{message}</p>}
      <div className="admin-row-actions">
        <button type="submit" className="btn btn-primary" disabled={saving}>
          {saving ? 'Saving…' : existing ? 'Save changes' : 'Create activity'}
        </button>
        {existing && (
          <button type="button" className="btn btn-outline" disabled={saving} onClick={() => void removeActivity()}>
            Remove
          </button>
        )}
      </div>
    </form>
  )
}
