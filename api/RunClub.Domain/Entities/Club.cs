namespace RunClub.Domain.Entities;

public class Club
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ClubMembership> Memberships { get; set; } = new List<ClubMembership>();
    public ICollection<ValidateMember> ValidateMembers { get; set; } = new List<ValidateMember>();
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
    public ICollection<TrainingGroup> TrainingGroups { get; set; } = new List<TrainingGroup>();
    public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    public ICollection<VolunteerRoleType> VolunteerRoleTypes { get; set; } = new List<VolunteerRoleType>();
}
