namespace RunClub.Domain.Entities;

public class TrainingGoal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MemberProfileId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? TargetTime { get; set; }
    public DateTime? TargetDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public MemberProfile MemberProfile { get; set; } = null!;
}
