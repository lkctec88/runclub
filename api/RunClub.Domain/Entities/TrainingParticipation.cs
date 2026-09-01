namespace RunClub.Domain.Entities;

public class TrainingParticipation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ActivityId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ParticipationMode Mode { get; set; }
    public bool Completed { get; set; }
    public double? DistanceMiles { get; set; }
    public int? TimeMinutes { get; set; }
    public string? Effort { get; set; }
    public string? Notes { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public Activity Activity { get; set; } = null!;
}
