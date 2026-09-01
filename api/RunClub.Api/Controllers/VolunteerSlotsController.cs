using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RunClub.Application.Abstractions;
using RunClub.Domain;
using RunClub.Domain.Entities;
using RunClub.Infrastructure.Persistence;

namespace RunClub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/activities/{activityId:guid}/volunteer-slots")]
public class VolunteerSlotsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IClubAuthorizationService _auth;

    public VolunteerSlotsController(AppDbContext db, IClubAuthorizationService auth)
    {
        _db = db;
        _auth = auth;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public record SlotRequest(string Role, string? Description, string? Requirements, string? Notes);

    [HttpGet]
    public async Task<ActionResult> List(Guid activityId)
    {
        var activity = await _db.Activities.FindAsync(activityId);
        if (activity is null || !activity.IsActive) return NotFound();
        if (activity.Kind == ActivityKind.PersonalActivity) return BadRequest("Personal activities cannot have volunteer slots");
        if (activity.ClubId.HasValue) await _auth.EnsureClubMemberAsync(UserId, activity.ClubId.Value);

        return Ok(await _db.VolunteerSlots
            .Where(s => s.ActivityId == activityId)
            .Select(s => new
            {
                s.Id,
                s.ActivityId,
                s.Role,
                s.Description,
                s.Requirements,
                s.AssignedUserId,
                s.Status
            })
            .ToListAsync());
    }

    [HttpPost]
    public async Task<ActionResult> Create(Guid activityId, SlotRequest req)
    {
        var activity = await _db.Activities.FindAsync(activityId);
        if (activity is null || !activity.IsActive) return NotFound();
        if (activity.Kind is not (ActivityKind.ClubActivity or ActivityKind.Race))
            return BadRequest("Volunteer slots only on club activities and races");
        if (!activity.ClubId.HasValue) return BadRequest();
        await _auth.EnsureClubAdminAsync(UserId, activity.ClubId.Value);

        var slot = new VolunteerSlot
        {
            ActivityId = activityId,
            Role = req.Role,
            Description = req.Description,
            Requirements = req.Requirements,
            Notes = req.Notes
        };
        _db.VolunteerSlots.Add(slot);
        await _db.SaveChangesAsync();
        return Ok(SlotDto(slot));
    }

    [HttpPost("{slotId:guid}/claim")]
    public async Task<ActionResult> Claim(Guid activityId, Guid slotId)
    {
        var activity = await _db.Activities.FindAsync(activityId);
        if (activity is null || !activity.ClubId.HasValue) return NotFound();
        await _auth.EnsureClubMemberAsync(UserId, activity.ClubId.Value);

        var slot = await _db.VolunteerSlots.FirstOrDefaultAsync(s => s.Id == slotId && s.ActivityId == activityId);
        if (slot is null) return NotFound();
        if (slot.Status != VolunteerSlotStatus.Available)
            return Conflict("Slot not available");

        slot.Status = VolunteerSlotStatus.Claimed;
        slot.AssignedUserId = UserId;

        var profile = await _db.MemberProfiles.FirstOrDefaultAsync(p => p.UserId == UserId);
        if (profile is not null) profile.VolunteerShifts++;

        await _db.SaveChangesAsync();
        return Ok(SlotDto(slot));
    }

    [HttpPost("{slotId:guid}/release")]
    public async Task<ActionResult> Release(Guid activityId, Guid slotId)
    {
        var activity = await _db.Activities.FindAsync(activityId);
        if (activity is null || !activity.ClubId.HasValue) return NotFound();
        await _auth.EnsureClubMemberAsync(UserId, activity.ClubId.Value);

        var slot = await _db.VolunteerSlots.FirstOrDefaultAsync(s => s.Id == slotId && s.ActivityId == activityId);
        if (slot is null) return NotFound();
        if (slot.AssignedUserId != UserId) return Forbid();
        if (slot.Status != VolunteerSlotStatus.Claimed)
            return BadRequest("Only claimed slots can be released");

        slot.Status = VolunteerSlotStatus.Available;
        slot.AssignedUserId = null;

        var profile = await _db.MemberProfiles.FirstOrDefaultAsync(p => p.UserId == UserId);
        if (profile is not null && profile.VolunteerShifts > 0) profile.VolunteerShifts--;

        await _db.SaveChangesAsync();
        return Ok(SlotDto(slot));
    }

    [HttpPost("{slotId:guid}/complete")]
    public async Task<ActionResult> Complete(Guid activityId, Guid slotId)
    {
        var activity = await _db.Activities.FindAsync(activityId);
        if (activity is null || !activity.ClubId.HasValue) return NotFound();
        await _auth.EnsureClubAdminAsync(UserId, activity.ClubId.Value);

        var slot = await _db.VolunteerSlots.FirstOrDefaultAsync(s => s.Id == slotId && s.ActivityId == activityId);
        if (slot is null) return NotFound();
        slot.Status = VolunteerSlotStatus.Completed;
        if (slot.Role.Contains("Leader", StringComparison.OrdinalIgnoreCase) && slot.AssignedUserId is not null)
        {
            var profile = await _db.MemberProfiles.FirstOrDefaultAsync(p => p.UserId == slot.AssignedUserId);
            if (profile is not null) profile.ActivitiesLed++;
        }

        await _db.SaveChangesAsync();
        return Ok(SlotDto(slot));
    }

    [HttpDelete("{slotId:guid}")]
    public async Task<ActionResult> Delete(Guid activityId, Guid slotId)
    {
        var activity = await _db.Activities.FindAsync(activityId);
        if (activity is null || !activity.ClubId.HasValue) return NotFound();
        await _auth.EnsureClubAdminAsync(UserId, activity.ClubId.Value);
        var slot = await _db.VolunteerSlots.FirstOrDefaultAsync(s => s.Id == slotId && s.ActivityId == activityId);
        if (slot is null) return NotFound();
        _db.VolunteerSlots.Remove(slot);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static object SlotDto(VolunteerSlot s) => new
    {
        s.Id,
        s.ActivityId,
        s.Role,
        s.Description,
        s.Requirements,
        s.AssignedUserId,
        s.Status
    };
}
