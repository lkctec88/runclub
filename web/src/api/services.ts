import { apiFetch, downloadFile } from './client'
import type {
  AuthResponse,
  CalendarItem,
  ChatMessage,
  Club,
  ClubMember,
  ValidateMember,
  CsvImportResult,
  MemberProfile,
  ProfileContributions,
  ActivityKind,
  GoingMember,
  ActivitySummary,
  TrainingGoal,
  TrainingGroup,
  VolunteerSlot,
  VolunteerRoleType,
} from '../types'

export const authApi = {
  login: (email: string, password: string) =>
    apiFetch<AuthResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),
  register: (data: {
    firstName: string
    lastName: string
    email: string
    password: string
    englandAthleticsNumber: string
  }) =>
    apiFetch<AuthResponse>('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  me: () => apiFetch<AuthResponse>('/api/auth/me'),
}

export const clubsApi = {
  list: () => apiFetch<Club[]>('/api/clubs'),
  get: (id: string) => apiFetch<Club>(`/api/clubs/${id}`),
  create: (data: { name: string; description?: string; location?: string }) =>
    apiFetch<Club>('/api/clubs', { method: 'POST', body: JSON.stringify(data) }),
  members: (clubId: string) => apiFetch<ClubMember[]>(`/api/clubs/${clubId}/members`),
  validateMembers: (clubId: string) =>
    apiFetch<ValidateMember[]>(`/api/clubs/${clubId}/validate-members`),
  addValidateMember: (clubId: string, data: {
    firstName: string
    lastName: string
    englandAthleticsNumber: string
    role: number
  }) =>
    apiFetch<{
      id: string
      firstName: string
      lastName: string
      englandAthleticsNumber: string
      role: number
      registered: boolean
    }>(`/api/clubs/${clubId}/validate-members`, { method: 'POST', body: JSON.stringify(data) }),
  removeValidateMember: (clubId: string, id: string) =>
    apiFetch(`/api/clubs/${clubId}/validate-members/${id}`, { method: 'DELETE' }),
  downloadMembersTemplate: (clubId: string) =>
    downloadFile(`/api/clubs/${clubId}/members/import-template`, 'members-template.csv'),
  updateMember: (clubId: string, membershipId: string, role: number) =>
    apiFetch(`/api/clubs/${clubId}/members/${membershipId}`, {
      method: 'PUT',
      body: JSON.stringify({ role }),
    }),
  setMemberActive: (clubId: string, membershipId: string, isActive: boolean) =>
    apiFetch(`/api/clubs/${clubId}/members/${membershipId}/status`, {
      method: 'PUT',
      body: JSON.stringify({ isActive }),
    }),
  setValidateMemberActive: (clubId: string, id: string, isActive: boolean) =>
    apiFetch(`/api/clubs/${clubId}/validate-members/${id}/status`, {
      method: 'PUT',
      body: JSON.stringify({ isActive }),
    }),
  removeMember: (clubId: string, membershipId: string) =>
    apiFetch(`/api/clubs/${clubId}/members/${membershipId}`, { method: 'DELETE' }),
  importMembers: (clubId: string, file: File, dryRun: boolean) => {
    const form = new FormData()
    form.append('file', file)
    return apiFetch<CsvImportResult>(`/api/clubs/${clubId}/members/import?dryRun=${dryRun}`, {
      method: 'POST',
      body: form,
    })
  },
  bulkDeleteMembers: (clubId: string, file: File, dryRun: boolean) => {
    const form = new FormData()
    form.append('file', file)
    return apiFetch<unknown>(`/api/clubs/${clubId}/members/bulk-delete?dryRun=${dryRun}`, {
      method: 'POST',
      body: form,
    })
  },
  volunteerRoleTypes: (clubId: string, includeInactive = false) => {
    const q = includeInactive ? '?includeInactive=true' : ''
    return apiFetch<VolunteerRoleType[]>(`/api/clubs/${clubId}/volunteer-role-types${q}`)
  },
  createVolunteerRoleType: (clubId: string, data: { name: string; description?: string }) =>
    apiFetch<VolunteerRoleType>(`/api/clubs/${clubId}/volunteer-role-types`, {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  updateVolunteerRoleType: (
    clubId: string,
    id: string,
    data: { name: string; description?: string; isActive: boolean },
  ) =>
    apiFetch<VolunteerRoleType>(`/api/clubs/${clubId}/volunteer-role-types/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    }),
}

export const activitiesApi = {
  list: (params?: { clubId?: string; kind?: ActivityKind; trainingOnly?: boolean }) => {
    const q = new URLSearchParams()
    if (params?.clubId) q.set('clubId', params.clubId)
    if (params?.kind !== undefined) q.set('kind', String(params.kind))
    if (params?.trainingOnly) q.set('trainingOnly', 'true')
    const qs = q.toString()
    return apiFetch<ActivitySummary[]>(`/api/activities${qs ? `?${qs}` : ''}`)
  },
  get: (id: string) => apiFetch<ActivitySummary & { volunteerSlots?: VolunteerSlot[] }>(`/api/activities/${id}`),
  listGoing: (id: string) => apiFetch<GoingMember[]>(`/api/activities/${id}/going`),
  create: (data: Record<string, unknown>) =>
    apiFetch<ActivitySummary>('/api/activities', { method: 'POST', body: JSON.stringify(data) }),
  update: (id: string, data: Record<string, unknown>) =>
    apiFetch<ActivitySummary>(`/api/activities/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  remove: (id: string) => apiFetch(`/api/activities/${id}`, { method: 'DELETE' }),
  setAttendance: (id: string, status: number, paceGroup?: string) =>
    apiFetch(`/api/activities/${id}/attendance`, {
      method: 'POST',
      body: JSON.stringify({ status, paceGroup }),
    }),
  confirmAttendance: (id: string, attended: boolean) =>
    apiFetch<{ status: number; attended: boolean }>(`/api/activities/${id}/attendance/confirm`, {
      method: 'POST',
      body: JSON.stringify({ attended }),
    }),
  rate: (id: string, data: { overallRating: number; comments?: string | null }) =>
    apiFetch(`/api/activities/${id}/ratings`, { method: 'POST', body: JSON.stringify(data) }),
  skipRating: (id: string) =>
    apiFetch(`/api/activities/${id}/ratings/skip`, { method: 'POST' }),
  trainingParticipation: (id: string, data: Record<string, unknown>) =>
    apiFetch(`/api/activities/${id}/training-participation`, { method: 'POST', body: JSON.stringify(data) }),
}

export const volunteerApi = {
  list: (activityId: string) => apiFetch<VolunteerSlot[]>(`/api/activities/${activityId}/volunteer-slots`),
  claim: (activityId: string, slotId: string) =>
    apiFetch<VolunteerSlot>(`/api/activities/${activityId}/volunteer-slots/${slotId}/claim`, { method: 'POST' }),
  release: (activityId: string, slotId: string) =>
    apiFetch<VolunteerSlot>(`/api/activities/${activityId}/volunteer-slots/${slotId}/release`, { method: 'POST' }),
  create: (activityId: string, data: { role: string; tag?: string; description?: string }) =>
    apiFetch<VolunteerSlot>(`/api/activities/${activityId}/volunteer-slots`, {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  remove: (activityId: string, slotId: string) =>
    apiFetch(`/api/activities/${activityId}/volunteer-slots/${slotId}`, { method: 'DELETE' }),
}

export const profileApi = {
  me: () => apiFetch<MemberProfile>('/api/profiles/me'),
  contributions: () => apiFetch<ProfileContributions>('/api/profiles/me/contributions'),
  update: (data: Partial<MemberProfile>) =>
    apiFetch<MemberProfile>('/api/profiles/me', { method: 'PUT', body: JSON.stringify(data) }),
  addGoal: (data: { label: string; targetTime?: string; targetDate?: string; isActive: boolean }) =>
    apiFetch<TrainingGoal>('/api/profiles/me/goals', { method: 'POST', body: JSON.stringify(data) }),
  clubProfiles: (clubId: string) => apiFetch<MemberProfile[]>(`/api/clubs/${clubId}/profiles`),
  findRunners: (clubId: string) => apiFetch<{ profile: MemberProfile; score: number }[]>(`/api/clubs/${clubId}/find-your-runners`),
  calendar: (clubId: string, params?: { from?: string; to?: string }) => {
    const q = new URLSearchParams()
    if (params?.from) q.set('from', params.from)
    if (params?.to) q.set('to', params.to)
    const qs = q.toString()
    return apiFetch<CalendarItem[]>(`/api/clubs/${clubId}/calendar${qs ? `?${qs}` : ''}`)
  },
  chat: (clubId: string) => apiFetch<ChatMessage[]>(`/api/clubs/${clubId}/chat/messages`),
  trainingGroups: (clubId: string) => apiFetch<TrainingGroup[]>(`/api/clubs/${clubId}/training-groups`),
  joinGroup: (clubId: string, groupId: string) =>
    apiFetch(`/api/clubs/${clubId}/training-groups/${groupId}/join`, { method: 'POST' }),
  createGroup: (clubId: string, data: Record<string, unknown>) =>
    apiFetch<TrainingGroup>(`/api/clubs/${clubId}/training-groups`, {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  createFeedToken: () => apiFetch<{ token: string; url: string }>('/api/calendar/feed-token', { method: 'POST' }),
}
