using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RunClub.Application.Abstractions;
using RunClub.Domain;
using RunClub.Infrastructure.Identity;
using RunClub.Infrastructure.Persistence;

namespace RunClub.Infrastructure.Services;

public class ClubAuthorizationService : IClubAuthorizationService
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;

    public ClubAuthorizationService(AppDbContext db, UserManager<AppUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<bool> IsSuperAdminAsync(string userId)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user?.PlatformRole == PlatformRole.SuperAdmin) return true;
        return await _db.ClubMemberships.AnyAsync(m =>
            m.UserId == userId && m.IsActive && m.Role == ClubRole.SuperAdmin);
    }

    public async Task<bool> IsClubAdminAsync(string userId, Guid clubId)
    {
        if (await IsSuperAdminAsync(userId)) return true;
        return await _db.ClubMemberships.AnyAsync(m =>
            m.UserId == userId && m.ClubId == clubId && m.IsActive && (m.Role == ClubRole.Admin || m.Role == ClubRole.SuperAdmin));
    }

    public async Task<bool> IsClubMemberAsync(string userId, Guid clubId)
    {
        if (await IsSuperAdminAsync(userId)) return true;
        return await _db.ClubMemberships.AnyAsync(m =>
            m.UserId == userId && m.ClubId == clubId && m.IsActive);
    }

    public async Task EnsureClubMemberAsync(string userId, Guid clubId)
    {
        if (!await IsClubMemberAsync(userId, clubId))
            throw new UnauthorizedAccessException("Club membership required.");
    }

    public async Task EnsureClubAdminAsync(string userId, Guid clubId)
    {
        if (!await IsClubAdminAsync(userId, clubId))
            throw new UnauthorizedAccessException("Club admin required.");
    }

    public async Task EnsureSuperAdminAsync(string userId)
    {
        if (!await IsSuperAdminAsync(userId))
            throw new UnauthorizedAccessException("SuperAdmin required.");
    }
}
