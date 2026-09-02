namespace RunClub.Domain.Entities;

public class VolunteerSlot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ActivityId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? Tag { get; set; }
    public string? Description { get; set; }
    public string? Requirements { get; set; }
    public string? Notes { get; set; }
    public string? AssignedUserId { get; set; }
    public VolunteerSlotStatus Status { get; set; } = VolunteerSlotStatus.Available;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Activity Activity { get; set; } = null!;
}
