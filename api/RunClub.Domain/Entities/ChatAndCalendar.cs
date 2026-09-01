namespace RunClub.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClubId { get; set; }
    public string SenderUserId { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public bool IsFlagged { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Club Club { get; set; } = null!;
}

public class CalendarFeedToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAtUtc { get; set; }
}
