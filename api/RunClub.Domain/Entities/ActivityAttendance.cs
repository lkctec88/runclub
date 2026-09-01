namespace RunClub.Domain.Entities;

public class ActivityAttendance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ActivityId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Going;
    public bool? Attended { get; set; }
    public DateTime? AttendedAtUtc { get; set; }
    public bool RatingSkipped { get; set; }
    public string? PaceGroup { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public Activity Activity { get; set; } = null!;
}
