export const PlatformRole = { User: 0, SuperAdmin: 1 } as const
export type PlatformRole = (typeof PlatformRole)[keyof typeof PlatformRole]

export const ClubRole = { Member: 0, Admin: 1, SuperAdmin: 2 } as const
export type ClubRole = (typeof ClubRole)[keyof typeof ClubRole]

export function clubRoleLabel(role: ClubRole) {
  if (role === ClubRole.SuperAdmin) return 'SuperAdmin'
  if (role === ClubRole.Admin) return 'Admin'
  return 'Member'
}

export const ActivityKind = { ClubActivity: 0, Race: 1, PersonalActivity: 2 } as const
export type ActivityKind = (typeof ActivityKind)[keyof typeof ActivityKind]

export const VolunteerSlotStatus = { Available: 0, Claimed: 1, Completed: 2 } as const
export type VolunteerSlotStatus = (typeof VolunteerSlotStatus)[keyof typeof VolunteerSlotStatus]

export interface MemberProfile {
  id: string
  userId: string
  firstName: string
  lastName: string
  photoUrl?: string
  bio?: string
  typicalPace?: string
  preferredDistances?: string
  preferredRunDays?: string
  runningExperience?: string
  currentRace?: string
  englandAthleticsNumber?: string
  activitiesCompleted: number
  volunteerShifts: number
  activitiesLed: number
  trainingSessionsCompleted: number
  trainingGoals?: TrainingGoal[]
}

export interface TrainingGoal {
  id: string
  label: string
  targetTime?: string
  targetDate?: string
  isActive: boolean
}

export interface ClubMembership {
  clubId: string
  role: ClubRole
}

export interface AuthUser {
  id: string
  email: string
  platformRole: PlatformRole
  profile?: MemberProfile
  memberships: ClubMembership[]
}

export interface AuthResponse {
  token: string
  user: AuthUser
}

export interface Club {
  id: string
  name: string
  description?: string
  location?: string
  logoUrl?: string
}

export interface ClubMember {
  id: string
  userId: string
  role: ClubRole
  joinedAtUtc: string
  isActive: boolean
  email?: string
  firstName: string
  lastName: string
  englandAthleticsNumber?: string
  typicalPace?: string
  photoUrl?: string
}

export interface ValidateMember {
  id: string
  firstName: string
  lastName: string
  englandAthleticsNumber: string
  role: ClubRole
  claimedUserId?: string
  createdAtUtc: string
  claimedAtUtc?: string
  isActive: boolean
}

export interface CsvImportResult {
  dryRun: boolean
  added: number
  updated: number
  removed: number
  skipped: number
  rows: { row: number; identifier: string; action: string; error?: string }[]
}

export const RecurrenceFrequency = { None: 0, Weekly: 1, Monthly: 2 } as const
export type RecurrenceFrequency = (typeof RecurrenceFrequency)[keyof typeof RecurrenceFrequency]

export const TrainingSessionType = {
  Hills: 0,
  TrackIntervals: 1,
  Tempo: 2,
  Fartlek: 3,
  SpeedWork: 4,
  Other: 5,
} as const
export type TrainingSessionType = (typeof TrainingSessionType)[keyof typeof TrainingSessionType]

export const AttendanceStatus = { Going: 0, Interested: 1, NotGoing: 2 } as const
export type AttendanceStatus = (typeof AttendanceStatus)[keyof typeof AttendanceStatus]

export interface ActivityAttendance {
  status: AttendanceStatus | string
  isGoing?: boolean
  attended?: boolean | null
  checkedIn?: boolean
  ratingSkipped?: boolean
}

export interface GoingMember {
  userId: string
  firstName: string
  lastName: string
  typicalPace?: string
  photoUrl?: string
}

export interface ActivitySummary {
  id: string
  clubId?: string
  kind: ActivityKind
  title: string
  description?: string
  startsAtUtc: string
  endsAtUtc?: string
  meetingPoint?: string
  location?: string
  route?: string
  distanceMiles?: string
  paceGroups?: string
  runType?: string
  maxCapacity?: number
  isTrainingSession: boolean
  sessionType?: number
  workoutInstructions?: string
  targetPaceOrEffort?: string
  virtualParticipationEnabled: boolean
  goingCount?: number
  goingMembers?: GoingMember[]
  availableSlots?: number
  claimedSlots?: number
  volunteerSlots?: VolunteerSlot[]
  tags?: string[]
  myAttendance?: ActivityAttendance | null
  hasRated?: boolean
  myRating?: {
    overallRating: number
    comments?: string | null
  } | null
}

export interface VolunteerRoleType {
  id: string
  clubId: string
  name: string
  description?: string
  isActive: boolean
  createdAtUtc: string
}

export interface VolunteerSlot {
  id: string
  activityId: string
  role: string
  tag?: string
  description?: string
  requirements?: string
  assignedUserId?: string
  assignedUserName?: string
  status: VolunteerSlotStatus
}

export function volunteerSlotLabel(slot: { role: string; tag?: string | null }) {
  const tag = slot.tag?.trim()
  return tag ? `${slot.role} · ${tag}` : slot.role
}

export interface CalendarItem {
  id: string
  title: string
  kind: ActivityKind
  isTrainingSession: boolean
  startsAtUtc: string
  meetingPoint?: string
  location?: string
  distanceMiles?: string
  paceGroups?: string
  virtualParticipationEnabled: boolean
  goingCount: number
  isGoing?: boolean
  tags?: string[]
  volunteerSlots: { id: string; role: string; tag?: string; status: VolunteerSlotStatus; assignedUserId?: string }[]
}

export interface ChatMessage {
  id: string
  clubId: string
  senderUserId: string
  senderName: string
  body: string
  createdAtUtc: string
  isDeleted: boolean
  isFlagged: boolean
}

export interface TrainingGroup {
  id: string
  clubId: string
  name: string
  targetTime?: string
  typicalPace?: string
  longRunDay?: string
  description?: string
  members: { id: string; userId: string }[]
}

export interface ContributionActivity {
  id: string
  title: string
  kind: ActivityKind
  isTrainingSession: boolean
  startsAtUtc: string
  meetingPoint?: string
  location?: string
  distanceMiles?: string
  paceGroups?: string
}

export interface ProfileContributions {
  activitiesSignedUp: { status: number; paceGroup?: string; activity: ContributionActivity }[]
  volunteerShifts: {
    id: string
    role: string
    tag?: string
    description?: string
    status: VolunteerSlotStatus
    activity: ContributionActivity
  }[]
  activitiesCompleted: { confirmedAtUtc: string; activity: ContributionActivity }[]
  activitiesLed: { source: string; role?: string; activity: ContributionActivity }[]
  trainingSessions: {
    mode: number
    distanceMiles?: number
    timeMinutes?: number
    effort?: string
    activity: ContributionActivity
  }[]
}

export function contributionRunToSummary(activity: ContributionActivity): ActivitySummary {
  return {
    id: activity.id,
    kind: activity.kind,
    title: activity.title,
    startsAtUtc: activity.startsAtUtc,
    meetingPoint: activity.meetingPoint,
    location: activity.location,
    distanceMiles: activity.distanceMiles,
    paceGroups: activity.paceGroups,
    isTrainingSession: activity.isTrainingSession,
    virtualParticipationEnabled: false,
  }
}

const activityKindLabels = ['Club Activity', 'Race', 'Personal'] as const

export function activityKindLabel(kind: ActivityKind) {
  return activityKindLabels[kind] ?? 'Activity'
}
