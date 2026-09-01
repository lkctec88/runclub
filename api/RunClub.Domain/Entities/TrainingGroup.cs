namespace RunClub.Domain.Entities;

public class TrainingGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClubId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TargetTime { get; set; }
    public string? TypicalPace { get; set; }
    public string? LongRunDay { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Club Club { get; set; } = null!;
    public ICollection<TrainingGroupMember> Members { get; set; } = new List<TrainingGroupMember>();
}

public class TrainingGroupMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TrainingGroupId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;

    public TrainingGroup TrainingGroup { get; set; } = null!;
}
