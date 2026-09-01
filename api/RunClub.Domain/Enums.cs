namespace RunClub.Domain;

public enum PlatformRole
{
    User = 0,
    SuperAdmin = 1
}

public enum ClubRole
{
    Member = 0,
    Admin = 1,
    SuperAdmin = 2
}

public enum ActivityKind
{
    ClubActivity = 0,
    Race = 1,
    PersonalActivity = 2
}

public enum AttendanceStatus
{
    Going = 0,
    Interested = 1,
    NotGoing = 2
}

public enum ParticipationMode
{
    InPerson = 0,
    Virtual = 1
}

public enum VolunteerSlotStatus
{
    Available = 0,
    Claimed = 1,
    Completed = 2
}

public enum TrainingSessionType
{
    Hills = 0,
    TrackIntervals = 1,
    Tempo = 2,
    Fartlek = 3,
    SpeedWork = 4,
    Other = 5
}
