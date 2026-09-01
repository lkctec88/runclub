namespace RunClub.Domain.Entities;

public class ClubMembership
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public Guid ClubId { get; set; }
    public ClubRole Role { get; set; } = ClubRole.Member;
    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public Club Club { get; set; } = null!;
}
