using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RunClub.Application.Abstractions;
using RunClub.Domain;
using RunClub.Domain.Entities;
using RunClub.Infrastructure.Identity;
using RunClub.Infrastructure.Persistence;

namespace RunClub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _users;
    private readonly AppDbContext _db;
    private readonly IJwtTokenService _jwt;

    public AuthController(UserManager<AppUser> users, AppDbContext db, IJwtTokenService jwt)
    {
        _users = users;
        _db = db;
        _jwt = jwt;
    }

    public record LoginRequest(string Email, string Password);
    public record RegisterRequest(string FirstName, string LastName, string Email, string Password, string EnglandAthleticsNumber);

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult> Login(LoginRequest req)
    {
        var user = await _users.FindByEmailAsync(req.Email);
        if (user is null || !user.IsActive || !await _users.CheckPasswordAsync(user, req.Password))
            return Unauthorized(new { message = "Invalid credentials" });

        if (user.PlatformRole != PlatformRole.SuperAdmin && !await HasActiveMembershipAsync(user.Id))
            return StatusCode(403, new { message = "Your club membership has lapsed. Contact your club to be reinstated." });

        return Ok(await BuildAuthResponse(user));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult> Register(RegisterRequest req)
    {
        var firstName = req.FirstName.Trim();
        var lastName = MembershipIdentity.NormalizeLastName(req.LastName);
        var ea = MembershipIdentity.NormalizeEnglandAthleticsNumber(req.EnglandAthleticsNumber);
        var email = (req.Email ?? string.Empty).Trim();
        if (firstName.Length == 0 || lastName.Length == 0 || email.Length == 0 || ea.Length == 0)
            return BadRequest(new { message = "First name, last name, email, and England Athletics number are required" });

        var emailUser = await _users.FindByEmailAsync(email);
        if (emailUser is not null && await _db.ClubMemberships.AnyAsync(m => m.UserId == emailUser.Id))
            return BadRequest(new { message = "An account with that email already exists. Sign in instead." });

        var existingProfile = (await _db.MemberProfiles
                .Where(p => p.EnglandAthleticsNumber != null)
                .ToListAsync())
            .FirstOrDefault(p => MembershipIdentity.NormalizeEnglandAthleticsNumber(p.EnglandAthleticsNumber) == ea);

        AppUser? eaUser = null;
        if (existingProfile is not null)
        {
            eaUser = await _users.FindByIdAsync(existingProfile.UserId);
            if (eaUser is not null && await _db.ClubMemberships.AnyAsync(m => m.UserId == eaUser.Id))
                return BadRequest(new { message = "An account with that England Athletics number already exists. Sign in instead." });
        }

        var existingUser = eaUser ?? (emailUser is not null && !await _db.ClubMemberships.AnyAsync(m => m.UserId == emailUser.Id) ? emailUser : null);

        var candidates = await _db.ValidateMembers.Where(i => i.IsActive).ToListAsync();
        var matches = candidates
            .Where(i =>
                MembershipIdentity.NormalizeEnglandAthleticsNumber(i.EnglandAthleticsNumber) == ea
                && MembershipIdentity.LastNamesMatch(i.LastName, lastName)
                && (string.IsNullOrEmpty(i.ClaimedUserId) || (existingUser is not null && i.ClaimedUserId == existingUser.Id)))
            .ToList();

        if (matches.Count == 0)
            return BadRequest(new { message = "Last name and England Athletics number do not match a club record" });

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            AppUser user;
            if (existingUser is null)
            {
                user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    PlatformRole = matches.Any(r => r.Role == ClubRole.SuperAdmin) ? PlatformRole.SuperAdmin : PlatformRole.User
                };
                var created = await _users.CreateAsync(user, req.Password);
                if (!created.Succeeded)
                {
                    await tx.RollbackAsync();
                    return BadRequest(new { message = string.Join(" ", created.Errors.Select(e => e.Description)) });
                }
            }
            else
            {
                user = existingUser;
                if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
                {
                    var emailTaken = await _users.FindByEmailAsync(email);
                    if (emailTaken is not null && emailTaken.Id != user.Id && await HasActiveMembershipAsync(emailTaken.Id))
                    {
                        await tx.RollbackAsync();
                        return BadRequest(new { message = "An account with that email already exists. Sign in instead." });
                    }
                    user.Email = email;
                    user.UserName = email;
                    user.NormalizedEmail = _users.NormalizeEmail(email);
                    user.NormalizedUserName = _users.NormalizeName(email);
                }

                user.EmailConfirmed = true;
                if (matches.Any(r => r.Role == ClubRole.SuperAdmin))
                    user.PlatformRole = PlatformRole.SuperAdmin;
            }

            if (existingProfile is null)
            {
                _db.MemberProfiles.Add(new MemberProfile
                {
                    UserId = user.Id,
                    FirstName = firstName,
                    LastName = lastName,
                    EnglandAthleticsNumber = ea
                });
            }
            else
            {
                existingProfile.UserId = user.Id;
                existingProfile.FirstName = firstName;
                existingProfile.LastName = lastName;
                existingProfile.EnglandAthleticsNumber = ea;
            }

            foreach (var row in matches)
            {
                row.ClaimedUserId = user.Id;
                row.ClaimedAtUtc = DateTime.UtcNow;
                var membership = await _db.ClubMemberships.FirstOrDefaultAsync(m => m.UserId == user.Id && m.ClubId == row.ClubId);
                if (membership is null)
                {
                    _db.ClubMemberships.Add(new ClubMembership
                    {
                        UserId = user.Id,
                        ClubId = row.ClubId,
                        Role = row.Role
                    });
                }
                else
                {
                    membership.IsActive = true;
                    membership.Role = row.Role;
                }
            }

            await _db.SaveChangesAsync();

            if (existingUser is not null)
            {
                var token = await _users.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _users.ResetPasswordAsync(user, token, req.Password);
                if (!passwordResult.Succeeded)
                {
                    await tx.RollbackAsync();
                    return BadRequest(new { message = string.Join(" ", passwordResult.Errors.Select(e => e.Description)) });
                }
            }

            await tx.CommitAsync();
            return Ok(await BuildAuthResponse(user));
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync();
            return BadRequest(new { message = "Could not create the account. If you already registered, sign in instead." });
        }
    }

    private Task<bool> HasActiveMembershipAsync(string userId)
        => _db.ClubMemberships.AnyAsync(m => m.UserId == userId && m.IsActive);

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _users.FindByIdAsync(userId);
        if (user is null) return Unauthorized();
        return Ok(await BuildAuthResponse(user));
    }

    private async Task<object> BuildAuthResponse(AppUser user)
    {
        var memberships = await _db.ClubMemberships
            .Where(m => m.UserId == user.Id && m.IsActive)
            .Select(m => new { m.ClubId, m.Role })
            .ToListAsync();

        var profile = await _db.MemberProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        var token = await _jwt.CreateTokenAsync(
            user.Id,
            user.Email!,
            user.PlatformRole,
            memberships.Select(m => (m.ClubId, m.Role)));

        return new
        {
            token,
            user = new
            {
                user.Id,
                user.Email,
                user.PlatformRole,
                profile,
                memberships
            }
        };
    }
}
