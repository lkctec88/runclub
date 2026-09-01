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
[Authorize]
[Route("api/clubs")]
public class ClubsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IClubAuthorizationService _auth;
    private readonly ICsvMemberService _csv;
    private readonly UserManager<AppUser> _users;

    public ClubsController(AppDbContext db, IClubAuthorizationService auth, ICsvMemberService csv, UserManager<AppUser> users)
    {
        _db = db;
        _auth = auth;
        _csv = csv;
        _users = users;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<ActionResult> List()
    {
        if (await _auth.IsSuperAdminAsync(UserId))
            return Ok(await _db.Clubs.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync());

        var clubIds = await _db.ClubMemberships
            .Where(m => m.UserId == UserId && m.IsActive)
            .Select(m => m.ClubId)
            .ToListAsync();

        return Ok(await _db.Clubs.Where(c => c.IsActive && clubIds.Contains(c.Id)).ToListAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult> Get(Guid id)
    {
        await _auth.EnsureClubMemberAsync(UserId, id);
        var club = await _db.Clubs.FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
        return club is null ? NotFound() : Ok(club);
    }

    public record ClubRequest(string Name, string? Description, string? Location, string? LogoUrl);

    [HttpPost]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult> Create(ClubRequest req)
    {
        var club = new Club
        {
            Name = req.Name,
            Description = req.Description,
            Location = req.Location,
            LogoUrl = req.LogoUrl
        };
        _db.Clubs.Add(club);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = club.Id }, club);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult> Update(Guid id, ClubRequest req)
    {
        var club = await _db.Clubs.FindAsync(id);
        if (club is null) return NotFound();
        club.Name = req.Name;
        club.Description = req.Description;
        club.Location = req.Location;
        club.LogoUrl = req.LogoUrl;
        await _db.SaveChangesAsync();
        return Ok(club);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var club = await _db.Clubs.FindAsync(id);
        if (club is null) return NotFound();
        club.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{clubId:guid}/members")]
    public async Task<ActionResult> Members(Guid clubId)
    {
        await _auth.EnsureClubMemberAsync(UserId, clubId);
        var members = await (
            from m in _db.ClubMemberships
            join p in _db.MemberProfiles on m.UserId equals p.UserId
            join u in _users.Users on m.UserId equals u.Id
            where m.ClubId == clubId
            orderby m.IsActive descending, p.LastName, p.FirstName
            select new
            {
                m.Id,
                m.UserId,
                m.Role,
                m.JoinedAtUtc,
                m.IsActive,
                u.Email,
                p.FirstName,
                p.LastName,
                p.EnglandAthleticsNumber,
                p.TypicalPace,
                p.PhotoUrl
            }
        ).ToListAsync();
        return Ok(members);
    }

    [HttpGet("{clubId:guid}/validate-members")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult> ValidateMembers(Guid clubId)
    {
        var rows = await _db.ValidateMembers
            .Where(i => i.ClubId == clubId)
            .OrderByDescending(i => i.IsActive)
            .ThenBy(i => i.LastName)
            .ThenBy(i => i.FirstName)
            .Select(i => new
            {
                i.Id,
                i.FirstName,
                i.LastName,
                i.EnglandAthleticsNumber,
                i.Role,
                i.ClaimedUserId,
                i.CreatedAtUtc,
                i.ClaimedAtUtc,
                i.IsActive
            })
            .ToListAsync();
        return Ok(rows);
    }

    public record AddValidateMemberRequest(string FirstName, string LastName, string EnglandAthleticsNumber, ClubRole Role);

    [HttpPost("{clubId:guid}/validate-members")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult> AddValidateMember(Guid clubId, AddValidateMemberRequest req)
    {
        var club = await _db.Clubs.FirstOrDefaultAsync(c => c.Id == clubId && c.IsActive);
        if (club is null) return NotFound();

        if (string.IsNullOrWhiteSpace(req.FirstName) || string.IsNullOrWhiteSpace(req.LastName) || string.IsNullOrWhiteSpace(req.EnglandAthleticsNumber))
            return BadRequest(new { message = "First name, last name, and England Athletics number are required" });

        var ea = MembershipIdentity.NormalizeEnglandAthleticsNumber(req.EnglandAthleticsNumber);
        if (ea.Length == 0)
            return BadRequest(new { message = "England Athletics number is invalid" });

        var row = await _db.ValidateMembers
            .Where(i => i.ClubId == clubId && i.EnglandAthleticsNumber == ea)
            .OrderByDescending(i => i.IsActive)
            .ThenByDescending(i => i.ClaimedUserId != null)
            .FirstOrDefaultAsync();

        if (row is null)
        {
            row = new ValidateMember
            {
                ClubId = clubId,
                FirstName = req.FirstName.Trim(),
                LastName = req.LastName.Trim(),
                EnglandAthleticsNumber = ea,
                Role = req.Role
            };
            _db.ValidateMembers.Add(row);
        }
        else if (row.ClaimedUserId is not null)
        {
            return Conflict(new { message = "This England Athletics number is already registered. Use the Active checkbox to restore a lapsed member." });
        }
        else
        {
            row.FirstName = req.FirstName.Trim();
            row.LastName = req.LastName.Trim();
            row.Role = req.Role;
            row.IsActive = true;
        }

        await _db.SaveChangesAsync();
        return Ok(new
        {
            row.Id,
            row.FirstName,
            row.LastName,
            row.EnglandAthleticsNumber,
            row.Role,
            registered = row.ClaimedUserId is not null
        });
    }

    public record UpdateMemberRequest(ClubRole Role);

    [HttpPut("{clubId:guid}/members/{membershipId:guid}")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult> UpdateMember(Guid clubId, Guid membershipId, UpdateMemberRequest req)
    {
        var membership = await _db.ClubMemberships.FirstOrDefaultAsync(m => m.Id == membershipId && m.ClubId == clubId);
        if (membership is null) return NotFound();
        membership.Role = req.Role;

        var user = await _users.FindByIdAsync(membership.UserId);
        if (user is not null)
        {
            var keepSuperAdmin = req.Role == ClubRole.SuperAdmin
                || await _db.ClubMemberships.AnyAsync(m =>
                    m.UserId == user.Id && m.IsActive && m.Id != membership.Id && m.Role == ClubRole.SuperAdmin);
            user.PlatformRole = keepSuperAdmin ? PlatformRole.SuperAdmin : PlatformRole.User;
        }

        await _db.SaveChangesAsync();
        return Ok(new { membership.Id, membership.UserId, membership.Role, membership.IsActive });
    }

    public record MembershipStatusRequest(bool IsActive);

    [HttpPut("{clubId:guid}/members/{membershipId:guid}/status")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult> SetMemberStatus(Guid clubId, Guid membershipId, MembershipStatusRequest req)
    {
        var membership = await _db.ClubMemberships.FirstOrDefaultAsync(m => m.Id == membershipId && m.ClubId == clubId);
        if (membership is null) return NotFound();

        membership.IsActive = req.IsActive;
        var invite = await _db.ValidateMembers.FirstOrDefaultAsync(i =>
            i.ClubId == clubId && i.ClaimedUserId == membership.UserId);
        if (invite is not null)
            invite.IsActive = req.IsActive;

        await _db.SaveChangesAsync();
        return Ok(new { membership.Id, membership.IsActive });
    }

    [HttpPut("{clubId:guid}/validate-members/{id:guid}/status")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult> SetValidateMemberStatus(Guid clubId, Guid id, MembershipStatusRequest req)
    {
        var row = await _db.ValidateMembers.FirstOrDefaultAsync(i => i.Id == id && i.ClubId == clubId);
        if (row is null) return NotFound();

        row.IsActive = req.IsActive;
        if (row.ClaimedUserId is not null)
        {
            var membership = await _db.ClubMemberships.FirstOrDefaultAsync(m =>
                m.ClubId == clubId && m.UserId == row.ClaimedUserId);
            if (membership is not null)
                membership.IsActive = req.IsActive;
        }

        await _db.SaveChangesAsync();
        return Ok(new { row.Id, row.IsActive });
    }

    [HttpDelete("{clubId:guid}/members/{membershipId:guid}")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult> RemoveMember(Guid clubId, Guid membershipId)
    {
        var membership = await _db.ClubMemberships.FirstOrDefaultAsync(m => m.Id == membershipId && m.ClubId == clubId);
        if (membership is null) return NotFound();
        membership.IsActive = false;
        var invite = await _db.ValidateMembers.FirstOrDefaultAsync(i =>
            i.ClubId == clubId && i.ClaimedUserId == membership.UserId);
        if (invite is not null)
            invite.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{clubId:guid}/validate-members/{id:guid}")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult> RemoveValidateMember(Guid clubId, Guid id)
    {
        var row = await _db.ValidateMembers.FirstOrDefaultAsync(i => i.Id == id && i.ClubId == clubId);
        if (row is null) return NotFound();
        row.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{clubId:guid}/members/import-template")]
    [Authorize(Policy = "SuperAdmin")]
    public ActionResult ImportTemplate(Guid clubId)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(_csv.GetTemplateCsv());
        return File(bytes, "text/csv", "members-template.csv");
    }

    [HttpPost("{clubId:guid}/members/import")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult> Import(Guid clubId, IFormFile file, [FromQuery] bool dryRun = true)
    {
        await using var stream = file.OpenReadStream();
        var result = await _csv.ImportAsync(clubId, stream, dryRun);
        return Ok(result);
    }

    [HttpPost("{clubId:guid}/members/bulk-delete")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult> BulkDelete(Guid clubId, IFormFile file, [FromQuery] bool dryRun = true)
    {
        await using var stream = file.OpenReadStream();
        var result = await _csv.BulkDeleteAsync(clubId, stream, dryRun);
        return Ok(result);
    }
}
