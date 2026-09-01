namespace RunClub.Domain.Entities;

public class ValidateMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClubId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string EnglandAthleticsNumber { get; set; } = string.Empty;
    public ClubRole Role { get; set; } = ClubRole.Member;
    public string? ClaimedUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ClaimedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;

    public Club Club { get; set; } = null!;
}
