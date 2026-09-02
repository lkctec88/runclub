namespace RunClub.Domain.Entities;

public class ActivityTag
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ActivityId { get; set; }
    public string Label { get; set; } = string.Empty;

    public Activity Activity { get; set; } = null!;
}
