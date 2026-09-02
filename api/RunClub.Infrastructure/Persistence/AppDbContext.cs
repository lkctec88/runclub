using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RunClub.Domain.Entities;
using RunClub.Infrastructure.Identity;

namespace RunClub.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<ClubMembership> ClubMemberships => Set<ClubMembership>();
    public DbSet<ValidateMember> ValidateMembers => Set<ValidateMember>();
    public DbSet<MemberProfile> MemberProfiles => Set<MemberProfile>();
    public DbSet<TrainingGoal> TrainingGoals => Set<TrainingGoal>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<ActivityTag> ActivityTags => Set<ActivityTag>();
    public DbSet<VolunteerSlot> VolunteerSlots => Set<VolunteerSlot>();
    public DbSet<VolunteerRoleType> VolunteerRoleTypes => Set<VolunteerRoleType>();
    public DbSet<ActivityAttendance> ActivityAttendances => Set<ActivityAttendance>();
    public DbSet<ActivityCheckIn> ActivityCheckIns => Set<ActivityCheckIn>();
    public DbSet<ActivityCheckOut> ActivityCheckOuts => Set<ActivityCheckOut>();
    public DbSet<ActivityRating> ActivityRatings => Set<ActivityRating>();
    public DbSet<TrainingParticipation> TrainingParticipations => Set<TrainingParticipation>();
    public DbSet<TrainingGroup> TrainingGroups => Set<TrainingGroup>();
    public DbSet<TrainingGroupMember> TrainingGroupMembers => Set<TrainingGroupMember>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<CalendarFeedToken> CalendarFeedTokens => Set<CalendarFeedToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ClubMembership>()
            .HasIndex(x => new { x.UserId, x.ClubId })
            .IsUnique();

        builder.Entity<MemberProfile>()
            .HasIndex(x => x.UserId)
            .IsUnique();

        builder.Entity<MemberProfile>()
            .HasIndex(x => x.EnglandAthleticsNumber)
            .IsUnique()
            .HasFilter("\"EnglandAthleticsNumber\" IS NOT NULL");

        builder.Entity<ValidateMember>()
            .ToTable("ValidateMembers");

        builder.Entity<ValidateMember>()
            .HasIndex(x => new { x.ClubId, x.EnglandAthleticsNumber })
            .IsUnique()
            .HasFilter("\"IsActive\" = TRUE AND \"ClaimedUserId\" IS NULL");

        builder.Entity<ValidateMember>()
            .HasOne(x => x.Club)
            .WithMany(c => c.ValidateMembers)
            .HasForeignKey(x => x.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CalendarFeedToken>()
            .HasIndex(x => x.Token)
            .IsUnique();

        builder.Entity<ActivityAttendance>()
            .HasIndex(x => new { x.ActivityId, x.UserId })
            .IsUnique();

        builder.Entity<ActivityRating>()
            .HasIndex(x => new { x.ActivityId, x.UserId })
            .IsUnique();

        builder.Entity<ActivityCheckIn>()
            .HasIndex(x => new { x.ActivityId, x.UserId })
            .IsUnique();

        builder.Entity<ActivityCheckOut>()
            .HasIndex(x => new { x.ActivityId, x.UserId })
            .IsUnique();

        builder.Entity<TrainingGroupMember>()
            .HasIndex(x => new { x.TrainingGroupId, x.UserId })
            .IsUnique();

        builder.Entity<Activity>()
            .HasIndex(a => a.RecurrenceGroupId);

        builder.Entity<Activity>()
            .Property(a => a.DistanceMiles)
            .HasMaxLength(40);

        builder.Entity<Activity>()
            .HasMany(r => r.VolunteerSlots)
            .WithOne(s => s.Activity)
            .HasForeignKey(s => s.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Activity>()
            .HasMany(r => r.Tags)
            .WithOne(t => t.Activity)
            .HasForeignKey(t => t.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ActivityTag>()
            .Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Entity<ActivityTag>()
            .Property(t => t.Label)
            .HasMaxLength(40)
            .IsRequired();

        builder.Entity<ActivityTag>()
            .HasIndex(t => new { t.ActivityId, t.Label })
            .IsUnique();

        builder.Entity<VolunteerRoleType>()
            .HasOne(t => t.Club)
            .WithMany(c => c.VolunteerRoleTypes)
            .HasForeignKey(t => t.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<VolunteerRoleType>()
            .HasIndex(t => new { t.ClubId, t.Name })
            .IsUnique();
    }
}
