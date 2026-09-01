using Microsoft.AspNetCore.Identity;
using RunClub.Domain;

namespace RunClub.Infrastructure.Identity;
public class AppUser : IdentityUser
{
    public bool IsActive { get; set; } = true;
    public PlatformRole PlatformRole { get; set; } = PlatformRole.User;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
