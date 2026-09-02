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
[Route("api")]
public class ProfilesCalendarChatController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IClubAuthorizationService _auth;
    private readonly IIcsCalendarService _ics;

    public ProfilesCalendarChatController(AppDbContext db, IClubAuthorizationService auth, IIcsCalendarService ics)
    {
        _db = db;
        _auth = auth;
        _ics = ics;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("profiles/me")]
    public async Task<ActionResult> MyProfile()
    {
        var profile = await _db.MemberProfiles
            .Include(p => p.TrainingGoals)
            .FirstOrDefaultAsync(p => p.UserId == UserId);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpGet("profiles/me/contributions")]
    public async Task<ActionResult> MyContributions()
    {
        var volunteerShifts = await (
            from slot in _db.VolunteerSlots
            join activity in _db.Activities on slot.ActivityId equals activity.Id
            where slot.AssignedUserId == UserId
                  && slot.Status != VolunteerSlotStatus.Available
                  && activity.IsActive
            orderby activity.StartsAtUtc descending
            select new
            {
                slot.Id,
                slot.Role,
                slot.Tag,
                slot.Description,
                slot.Status,
                Activity = new
                {
                    activity.Id,
                    activity.Title,
                    activity.Kind,
                    activity.IsTrainingSession,
                    activity.StartsAtUtc,
                    activity.MeetingPoint,
                    activity.Location,
                    activity.DistanceMiles
                }
            }).ToListAsync();

        var confirmedCompleted = await (
            from attendance in _db.ActivityAttendances
            join activity in _db.Activities on attendance.ActivityId equals activity.Id
            where attendance.UserId == UserId && attendance.Attended == true && activity.IsActive
            orderby (attendance.AttendedAtUtc ?? attendance.UpdatedAtUtc) descending
            select new
            {
                ConfirmedAtUtc = attendance.AttendedAtUtc ?? attendance.UpdatedAtUtc,
                Activity = new
                {
                    activity.Id,
                    activity.Title,
                    activity.Kind,
                    activity.IsTrainingSession,
                    activity.StartsAtUtc,
                    activity.MeetingPoint,
                    activity.Location,
                    activity.DistanceMiles
                }
            }).ToListAsync();

        var confirmedActivityIds = confirmedCompleted.Select(x => x.Activity.Id).ToHashSet();
        var legacyCompleted = await (
            from checkout in _db.ActivityCheckOuts
            join activity in _db.Activities on checkout.ActivityId equals activity.Id
            where checkout.UserId == UserId && activity.IsActive && !confirmedActivityIds.Contains(activity.Id)
            orderby checkout.CheckedOutAtUtc descending
            select new
            {
                ConfirmedAtUtc = checkout.CheckedOutAtUtc,
                Activity = new
                {
                    activity.Id,
                    activity.Title,
                    activity.Kind,
                    activity.IsTrainingSession,
                    activity.StartsAtUtc,
                    activity.MeetingPoint,
                    activity.Location,
                    activity.DistanceMiles
                }
            }).ToListAsync();

        var activitiesCompleted = confirmedCompleted
            .Concat(legacyCompleted)
            .OrderByDescending(x => x.ConfirmedAtUtc)
            .ToList();

        var ledFromActivities = await _db.Activities
            .Where(r => r.IsActive && r.RunLeaderUserId == UserId)
            .OrderByDescending(r => r.StartsAtUtc)
            .Select(activity => new
            {
                Source = "activity-leader",
                Activity = new
                {
                    activity.Id,
                    activity.Title,
                    activity.Kind,
                    activity.IsTrainingSession,
                    activity.StartsAtUtc,
                    activity.MeetingPoint,
                    activity.Location,
                    activity.DistanceMiles
                }
            }).ToListAsync();

        var ledFromVolunteering = await (
            from slot in _db.VolunteerSlots
            join activity in _db.Activities on slot.ActivityId equals activity.Id
            where slot.AssignedUserId == UserId
                  && slot.Status == VolunteerSlotStatus.Completed
                  && slot.Role.Contains("Leader")
                  && activity.IsActive
            orderby activity.StartsAtUtc descending
            select new
            {
                Source = "volunteer-leader",
                Role = (string?)slot.Role,
                Activity = new
                {
                    activity.Id,
                    activity.Title,
                    activity.Kind,
                    activity.IsTrainingSession,
                    activity.StartsAtUtc,
                    activity.MeetingPoint,
                    activity.Location,
                    activity.DistanceMiles
                }
            }).ToListAsync();

        var activitiesLed = ledFromActivities
            .Select(x => new { x.Source, Role = (string?)null, x.Activity })
            .Concat(ledFromVolunteering.Select(x => new { x.Source, x.Role, x.Activity }))
            .OrderByDescending(x => x.Activity.StartsAtUtc)
            .ToList();

        var trainingSessions = await (
            from part in _db.TrainingParticipations
            join activity in _db.Activities on part.ActivityId equals activity.Id
            where part.UserId == UserId && part.Completed && activity.IsActive
            orderby activity.StartsAtUtc descending
            select new
            {
                part.Mode,
                part.DistanceMiles,
                part.TimeMinutes,
                part.Effort,
                Activity = new
                {
                    activity.Id,
                    activity.Title,
                    activity.Kind,
                    activity.IsTrainingSession,
                    activity.StartsAtUtc,
                    activity.MeetingPoint,
                    activity.Location,
                    activity.DistanceMiles
                }
            }).ToListAsync();

        var activitiesSignedUp = await (
            from attendance in _db.ActivityAttendances
            join activity in _db.Activities on attendance.ActivityId equals activity.Id
            where attendance.UserId == UserId
                  && attendance.Status == AttendanceStatus.Going
                  && attendance.Attended == null
                  && activity.IsActive
            orderby activity.StartsAtUtc
            select new
            {
                attendance.Status,
                attendance.PaceGroup,
                Activity = new
                {
                    activity.Id,
                    activity.Title,
                    activity.Kind,
                    activity.IsTrainingSession,
                    activity.StartsAtUtc,
                    activity.MeetingPoint,
                    activity.Location,
                    activity.DistanceMiles,
                    activity.PaceGroups
                }
            }).ToListAsync();

        return Ok(new
        {
            activitiesSignedUp,
            volunteerShifts,
            activitiesCompleted,
            activitiesLed,
            trainingSessions
        });
    }

    public record ProfileUpdateRequest(
        string FirstName,
        string LastName,
        string? PhotoUrl,
        string? Bio,
        string? TypicalPace,
        string? PreferredDistances,
        string? PreferredRunDays,
        string? RunningExperience,
        string? CurrentRace);

    [HttpPut("profiles/me")]
    public async Task<ActionResult> UpdateProfile(ProfileUpdateRequest req)
    {
        var profile = await _db.MemberProfiles.FirstOrDefaultAsync(p => p.UserId == UserId);
        if (profile is null) return NotFound();
        profile.FirstName = req.FirstName;
        profile.LastName = req.LastName;
        profile.PhotoUrl = req.PhotoUrl;
        profile.Bio = req.Bio;
        profile.TypicalPace = req.TypicalPace;
        profile.PreferredDistances = req.PreferredDistances;
        profile.PreferredRunDays = req.PreferredRunDays;
        profile.RunningExperience = req.RunningExperience;
        profile.CurrentRace = req.CurrentRace;
        profile.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(profile);
    }

    public record GoalRequest(string Label, string? TargetTime, DateTime? TargetDate, bool IsActive);

    [HttpPost("profiles/me/goals")]
    public async Task<ActionResult> AddGoal(GoalRequest req)
    {
        var profile = await _db.MemberProfiles.Include(p => p.TrainingGoals)
            .FirstOrDefaultAsync(p => p.UserId == UserId);
        if (profile is null) return NotFound();

        if (req.IsActive)
        {
            foreach (var g in profile.TrainingGoals) g.IsActive = false;
        }

        var goal = new TrainingGoal
        {
            MemberProfileId = profile.Id,
            Label = req.Label,
            TargetTime = req.TargetTime,
            TargetDate = req.TargetDate,
            IsActive = req.IsActive
        };
        _db.TrainingGoals.Add(goal);
        await _db.SaveChangesAsync();
        return Ok(goal);
    }

    [HttpGet("clubs/{clubId:guid}/profiles")]
    public async Task<ActionResult> ClubProfiles(Guid clubId)
    {
        await _auth.EnsureClubMemberAsync(UserId, clubId);
        var userIds = await _db.ClubMemberships
            .Where(m => m.ClubId == clubId && m.IsActive)
            .Select(m => m.UserId)
            .ToListAsync();
        var profiles = await _db.MemberProfiles
            .Include(p => p.TrainingGoals)
            .Where(p => userIds.Contains(p.UserId))
            .ToListAsync();
        return Ok(profiles);
    }

    [HttpGet("clubs/{clubId:guid}/find-your-runners")]
    public async Task<ActionResult> FindYourRunners(Guid clubId)
    {
        await _auth.EnsureClubMemberAsync(UserId, clubId);
        var me = await _db.MemberProfiles.Include(p => p.TrainingGoals)
            .FirstOrDefaultAsync(p => p.UserId == UserId);
        if (me is null) return Ok(Array.Empty<object>());

        var myGoal = me.TrainingGoals.FirstOrDefault(g => g.IsActive)?.Label;
        var userIds = await _db.ClubMemberships
            .Where(m => m.ClubId == clubId && m.IsActive && m.UserId != UserId)
            .Select(m => m.UserId)
            .ToListAsync();

        var candidates = await _db.MemberProfiles
            .Include(p => p.TrainingGoals)
            .Where(p => userIds.Contains(p.UserId))
            .ToListAsync();

        var matches = candidates
            .Select(p => new
            {
                profile = p,
                score =
                    (myGoal is not null && p.TrainingGoals.Any(g => g.IsActive && g.Label == myGoal) ? 2 : 0) +
                    (me.TypicalPace is not null && p.TypicalPace == me.TypicalPace ? 1 : 0) +
                    (me.PreferredDistances is not null && p.PreferredDistances == me.PreferredDistances ? 1 : 0)
            })
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .Take(10)
            .ToList();

        return Ok(matches);
    }

    [HttpGet("clubs/{clubId:guid}/training-groups")]
    public async Task<ActionResult> Groups(Guid clubId)
    {
        await _auth.EnsureClubMemberAsync(UserId, clubId);
        var groups = await _db.TrainingGroups
            .Include(g => g.Members)
            .Where(g => g.ClubId == clubId)
            .ToListAsync();
        return Ok(groups);
    }

    public record GroupRequest(string Name, string? TargetTime, string? TypicalPace, string? LongRunDay, string? Description);

    [HttpPost("clubs/{clubId:guid}/training-groups")]
    public async Task<ActionResult> CreateGroup(Guid clubId, GroupRequest req)
    {
        await _auth.EnsureClubMemberAsync(UserId, clubId);
        var group = new TrainingGroup
        {
            ClubId = clubId,
            Name = req.Name,
            TargetTime = req.TargetTime,
            TypicalPace = req.TypicalPace,
            LongRunDay = req.LongRunDay,
            Description = req.Description
        };
        group.Members.Add(new TrainingGroupMember { UserId = UserId });
        _db.TrainingGroups.Add(group);
        await _db.SaveChangesAsync();
        return Ok(group);
    }

    [HttpPost("clubs/{clubId:guid}/training-groups/{groupId:guid}/join")]
    public async Task<ActionResult> JoinGroup(Guid clubId, Guid groupId)
    {
        await _auth.EnsureClubMemberAsync(UserId, clubId);
        var group = await _db.TrainingGroups.Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId && g.ClubId == clubId);
        if (group is null) return NotFound();
        if (group.Members.All(m => m.UserId != UserId))
        {
            group.Members.Add(new TrainingGroupMember { UserId = UserId });
            await _db.SaveChangesAsync();
        }

        return Ok(group);
    }

    [HttpGet("clubs/{clubId:guid}/calendar")]
    public async Task<ActionResult> ClubCalendar(Guid clubId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        await _auth.EnsureClubMemberAsync(UserId, clubId);
        var start = from ?? DateTime.UtcNow.Date;
        var end = to ?? start.AddDays(14);

        var items = await _db.Activities
            .Where(r => r.IsActive && r.ClubId == clubId
                && (r.Kind == ActivityKind.ClubActivity || r.Kind == ActivityKind.Race)
                && r.StartsAtUtc >= start && r.StartsAtUtc < end)
            .OrderBy(r => r.StartsAtUtc)
            .Select(r => new
            {
                r.Id,
                r.Title,
                r.Kind,
                r.IsTrainingSession,
                r.StartsAtUtc,
                r.MeetingPoint,
                r.Location,
                r.DistanceMiles,
                r.PaceGroups,
                r.VirtualParticipationEnabled,
                Tags = r.Tags.OrderBy(t => t.Label).Select(t => t.Label).ToList(),
                GoingCount = r.Attendances.Count(a => a.Status == AttendanceStatus.Going),
                VolunteerSlots = r.VolunteerSlots.Select(s => new
                {
                    s.Id,
                    s.Role,
                    s.Tag,
                    s.Status,
                    s.AssignedUserId
                }).ToList()
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("calendar/feed-token")]
    public async Task<ActionResult> CreateFeedToken()
    {
        var existing = await _db.CalendarFeedTokens
            .Where(t => t.UserId == UserId && t.RevokedAtUtc == null)
            .ToListAsync();
        foreach (var t in existing) t.RevokedAtUtc = DateTime.UtcNow;

        var token = new CalendarFeedToken
        {
            UserId = UserId,
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .TrimEnd('=').Replace('+', '-').Replace('/', '_')
        };
        _db.CalendarFeedTokens.Add(token);
        await _db.SaveChangesAsync();

        var url = $"{Request.Scheme}://{Request.Host}/api/calendar/{token.Token}.ics";
        return Ok(new { token.Token, url });
    }

    [HttpGet("calendar/{token}.ics")]
    [AllowAnonymous]
    public async Task<IActionResult> PersonalFeed(string token)
    {
        var feed = await _db.CalendarFeedTokens
            .FirstOrDefaultAsync(t => t.Token == token && t.RevokedAtUtc == null);
        if (feed is null) return NotFound();
        var ics = await _ics.BuildPersonalFeedAsync(feed.UserId);
        return File(System.Text.Encoding.UTF8.GetBytes(ics), "text/calendar", "runclub.ics");
    }

    [HttpGet("activities/{id:guid}.ics")]
    [AllowAnonymous]
    public async Task<IActionResult> ActivityIcs(Guid id)
    {
        var ics = await _ics.BuildActivityEventAsync(id);
        return File(System.Text.Encoding.UTF8.GetBytes(ics), "text/calendar", $"activity-{id}.ics");
    }

    [HttpGet("clubs/{clubId:guid}/chat/messages")]
    public async Task<ActionResult> ChatMessages(Guid clubId, [FromQuery] int take = 50)
    {
        await _auth.EnsureClubMemberAsync(UserId, clubId);
        var messages = await (
            from m in _db.ChatMessages
            join p in _db.MemberProfiles on m.SenderUserId equals p.UserId into pj
            from p in pj.DefaultIfEmpty()
            where m.ClubId == clubId
            orderby m.CreatedAtUtc descending
            select new
            {
                m.Id,
                m.ClubId,
                m.SenderUserId,
                SenderName = p == null ? "Member" : p.FirstName + " " + p.LastName,
                m.Body,
                m.CreatedAtUtc,
                m.IsDeleted,
                m.IsFlagged
            }
        ).Take(take).ToListAsync();

        return Ok(messages.OrderBy(m => m.CreatedAtUtc));
    }
}
