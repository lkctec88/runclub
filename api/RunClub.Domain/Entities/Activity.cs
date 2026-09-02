namespace RunClub.Domain.Entities;

public class Activity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ActivityKind Kind { get; set; }
    public Guid? ClubId { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public string? MeetingPoint { get; set; }
    public string? Location { get; set; }
    public string? Route { get; set; }
    public string? DistanceMiles { get; set; }
    public string? PaceGroups { get; set; }
    public string? RunType { get; set; }
    public string? RunLeaderUserId { get; set; }
    public string? BackMarkerUserId { get; set; }
    public int? MaxCapacity { get; set; }
    public bool IsTrainingSession { get; set; }
    public TrainingSessionType? SessionType { get; set; }
    public string? WorkoutInstructions { get; set; }
    public string? TargetPaceOrEffort { get; set; }
    public string? CoachUserId { get; set; }
    public bool VirtualParticipationEnabled { get; set; }
    public Guid? RecurrenceGroupId { get; set; }
    public RecurrenceFrequency RecurrenceFrequency { get; set; }
    public DateTime? RecurrenceUntilUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Club? Club { get; set; }
    public ICollection<ActivityTag> Tags { get; set; } = new List<ActivityTag>();
    public ICollection<VolunteerSlot> VolunteerSlots { get; set; } = new List<VolunteerSlot>();
    public ICollection<ActivityAttendance> Attendances { get; set; } = new List<ActivityAttendance>();
    public ICollection<ActivityCheckIn> CheckIns { get; set; } = new List<ActivityCheckIn>();
    public ICollection<ActivityCheckOut> CheckOuts { get; set; } = new List<ActivityCheckOut>();
    public ICollection<ActivityRating> Ratings { get; set; } = new List<ActivityRating>();
    public ICollection<TrainingParticipation> TrainingParticipations { get; set; } = new List<TrainingParticipation>();
}
