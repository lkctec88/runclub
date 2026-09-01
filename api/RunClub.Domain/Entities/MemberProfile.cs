namespace RunClub.Domain.Entities;

public class MemberProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? EnglandAthleticsNumber { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Bio { get; set; }
    public string? TypicalPace { get; set; }
    public string? PreferredDistances { get; set; }
    public string? PreferredRunDays { get; set; }
    public string? RunningExperience { get; set; }
    public string? CurrentRace { get; set; }
    public int ActivitiesCompleted { get; set; }
    public int VolunteerShifts { get; set; }
    public int ActivitiesLed { get; set; }
    public int TrainingSessionsCompleted { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<TrainingGoal> TrainingGoals { get; set; } = new List<TrainingGoal>();
}
