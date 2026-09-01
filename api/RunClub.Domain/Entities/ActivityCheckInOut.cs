namespace RunClub.Domain.Entities;

public class ActivityCheckIn
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ActivityId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CheckedInAtUtc { get; set; } = DateTime.UtcNow;
    public string? PaceGroup { get; set; }

    public Activity Activity { get; set; } = null!;
}

public class ActivityCheckOut
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ActivityId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CheckedOutAtUtc { get; set; } = DateTime.UtcNow;

    public Activity Activity { get; set; } = null!;
}
