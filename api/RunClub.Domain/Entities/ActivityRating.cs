namespace RunClub.Domain.Entities;

public class ActivityRating
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ActivityId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int OverallRating { get; set; }
    public int Enjoyment { get; set; }
    public string PaceFeedback { get; set; } = "JustRight";
    public bool RouteThumbsUp { get; set; }
    public bool WouldDoAgain { get; set; }
    public string? Comments { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Activity Activity { get; set; } = null!;
}
