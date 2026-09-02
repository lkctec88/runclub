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
[Route("api/activities")]
public class ActivitiesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IClubAuthorizationService _auth;

    public ActivitiesController(AppDbContext db, IClubAuthorizationService auth)
    {
        _db = db;
        _auth = auth;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public record ActivityDto(
        Guid? ClubId,
        ActivityKind Kind,
        string Title,
        string? Description,
        DateTime StartsAtUtc,
        DateTime? EndsAtUtc,
        string? MeetingPoint,
        string? Location,
        string? Route,
        double? DistanceMiles,
        string? PaceGroups,
        string? RunType,
        string? RunLeaderUserId,
        string? BackMarkerUserId,
        int? MaxCapacity,
        bool IsTrainingSession,
        TrainingSessionType? SessionType,
        string? WorkoutInstructions,
        string? TargetPaceOrEffort,
        string? CoachUserId,
        bool VirtualParticipationEnabled,
        IReadOnlyList<string>? Tags = null,
        IReadOnlyList<VolunteerNeedRequest>? VolunteerNeeds = null,
        RecurrenceFrequency RecurrenceFrequency = RecurrenceFrequency.None,
        DateTime? RecurrenceUntilUtc = null);

    public record VolunteerNeedRequest(Guid VolunteerRoleTypeId, int Count, string? Tag = null);

    [HttpGet]
    public async Task<ActionResult> List([FromQuery] Guid? clubId, [FromQuery] ActivityKind? kind, [FromQuery] bool? trainingOnly)
    {
        var query = _db.Activities.AsQueryable().Where(r => r.IsActive);

        if (clubId.HasValue)
        {
            await _auth.EnsureClubMemberAsync(UserId, clubId.Value);
            query = query.Where(r => r.ClubId == clubId && (r.Kind == ActivityKind.ClubActivity || r.Kind == ActivityKind.Race));
        }
        else
        {
            query = query.Where(r =>
                r.Kind == ActivityKind.PersonalActivity && r.CreatedByUserId == UserId);
        }

        if (kind.HasValue) query = query.Where(r => r.Kind == kind);
        if (trainingOnly == true) query = query.Where(r => r.IsTrainingSession);

        var activities = await query.OrderBy(r => r.StartsAtUtc)
            .Select(r => new
            {
                r.Id,
                r.ClubId,
                r.Kind,
                r.Title,
                r.Description,
                r.StartsAtUtc,
                r.EndsAtUtc,
                r.MeetingPoint,
                r.Location,
                r.Route,
                r.DistanceMiles,
                r.PaceGroups,
                r.RunType,
                r.RunLeaderUserId,
                r.BackMarkerUserId,
                r.MaxCapacity,
                r.IsTrainingSession,
                r.SessionType,
                r.WorkoutInstructions,
                r.TargetPaceOrEffort,
                r.CoachUserId,
                r.VirtualParticipationEnabled,
                Tags = r.Tags.OrderBy(t => t.Label).Select(t => t.Label).ToList(),
                GoingCount = r.Attendances.Count(a => a.Status == AttendanceStatus.Going),
                GoingMembers = r.Attendances
                    .Where(a => a.Status == AttendanceStatus.Going)
                    .Join(_db.MemberProfiles, a => a.UserId, p => p.UserId, (a, p) => new
                    {
                        p.UserId,
                        p.FirstName,
                        p.LastName,
                        p.TypicalPace,
                        p.PhotoUrl
                    })
                    .OrderBy(p => p.FirstName)
                    .ThenBy(p => p.LastName)
                    .ToList(),
                AvailableSlots = r.VolunteerSlots.Count(s => s.Status == VolunteerSlotStatus.Available),
                ClaimedSlots = r.VolunteerSlots.Count(s => s.Status == VolunteerSlotStatus.Claimed),
                VolunteerSlots = r.VolunteerSlots.Select(s => new
                {
                    s.Id,
                    s.ActivityId,
                    s.Role,
                    s.Tag,
                    s.Description,
                    s.Requirements,
                    s.AssignedUserId,
                    s.Status
                }).ToList(),
                HasRated = r.Ratings.Any(x => x.UserId == UserId),
                MyRating = r.Ratings
                    .Where(x => x.UserId == UserId)
                    .Select(x => new { x.OverallRating, x.Comments })
                    .FirstOrDefault(),
                MyAttendance = r.Attendances
                    .Where(a => a.UserId == UserId)
                    .Select(a => new
                    {
                        a.Status,
                        IsGoing = a.Status == AttendanceStatus.Going,
                        a.Attended,
                        CheckedIn = a.Attended == true,
                        a.RatingSkipped
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(activities);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult> Get(Guid id)
    {
        var activity = await _db.Activities.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id && r.IsActive);
        if (activity is null) return NotFound();

        if (activity.Kind == ActivityKind.PersonalActivity && activity.CreatedByUserId != UserId && !await _auth.IsSuperAdminAsync(UserId))
            return Forbid();
        if (activity.ClubId.HasValue)
            await _auth.EnsureClubMemberAsync(UserId, activity.ClubId.Value);

        var dto = await _db.Activities
            .Where(r => r.Id == id && r.IsActive)
            .Select(r => new
            {
                r.Id,
                r.ClubId,
                r.Kind,
                r.Title,
                r.Description,
                r.StartsAtUtc,
                r.EndsAtUtc,
                r.MeetingPoint,
                r.Location,
                r.Route,
                r.DistanceMiles,
                r.PaceGroups,
                r.RunType,
                r.RunLeaderUserId,
                r.BackMarkerUserId,
                r.MaxCapacity,
                r.IsTrainingSession,
                r.SessionType,
                r.WorkoutInstructions,
                r.TargetPaceOrEffort,
                r.CoachUserId,
                r.VirtualParticipationEnabled,
                Tags = r.Tags.OrderBy(t => t.Label).Select(t => t.Label).ToList(),
                GoingCount = r.Attendances.Count(a => a.Status == AttendanceStatus.Going),
                GoingMembers = r.Attendances
                    .Where(a => a.Status == AttendanceStatus.Going)
                    .Join(_db.MemberProfiles, a => a.UserId, p => p.UserId, (a, p) => new
                    {
                        p.UserId,
                        p.FirstName,
                        p.LastName,
                        p.TypicalPace,
                        p.PhotoUrl
                    })
                    .OrderBy(p => p.FirstName)
                    .ThenBy(p => p.LastName)
                    .ToList(),
                AvailableSlots = r.VolunteerSlots.Count(s => s.Status == VolunteerSlotStatus.Available),
                ClaimedSlots = r.VolunteerSlots.Count(s => s.Status == VolunteerSlotStatus.Claimed),
                VolunteerSlots = r.VolunteerSlots.Select(s => new
                {
                    s.Id,
                    s.ActivityId,
                    s.Role,
                    s.Tag,
                    s.Description,
                    s.Requirements,
                    s.AssignedUserId,
                    s.Status
                }).ToList(),
                HasRated = r.Ratings.Any(x => x.UserId == UserId),
                MyRating = r.Ratings
                    .Where(x => x.UserId == UserId)
                    .Select(x => new { x.OverallRating, x.Comments })
                    .FirstOrDefault(),
                MyAttendance = r.Attendances
                    .Where(a => a.UserId == UserId)
                    .Select(a => new
                    {
                        a.Status,
                        IsGoing = a.Status == AttendanceStatus.Going,
                        a.Attended,
                        CheckedIn = a.Attended == true,
                        a.RatingSkipped
                    })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("{id:guid}/going")]
    public async Task<ActionResult> ListGoing(Guid id)
    {
        var activity = await _db.Activities.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id && r.IsActive);
        if (activity is null) return NotFound();

        if (activity.Kind == ActivityKind.PersonalActivity && activity.CreatedByUserId != UserId && !await _auth.IsSuperAdminAsync(UserId))
            return Forbid();
        if (activity.ClubId.HasValue)
            await _auth.EnsureClubMemberAsync(UserId, activity.ClubId.Value);

        var going = await (
            from a in _db.ActivityAttendances
            join p in _db.MemberProfiles on a.UserId equals p.UserId
            where a.ActivityId == id && a.Status == AttendanceStatus.Going
            orderby p.FirstName, p.LastName
            select new
            {
                p.UserId,
                p.FirstName,
                p.LastName,
                p.TypicalPace,
                p.PhotoUrl
            }).ToListAsync();

        return Ok(going);
    }

    [HttpPost]
    public async Task<ActionResult> Create(ActivityDto dto)
    {
        if (dto.Kind is ActivityKind.ClubActivity or ActivityKind.Race)
        {
            if (!dto.ClubId.HasValue) return BadRequest("ClubId required");
            await _auth.EnsureClubAdminAsync(UserId, dto.ClubId.Value);
        }

        if (dto.IsTrainingSession && dto.Kind != ActivityKind.ClubActivity)
            return BadRequest("Only club activities can be training sessions");

        var frequency = ActivitySchedule.CanRecur(dto.Kind) ? dto.RecurrenceFrequency : RecurrenceFrequency.None;
        if (frequency is RecurrenceFrequency.Weekly or RecurrenceFrequency.Monthly
            && dto.RecurrenceUntilUtc is { } until
            && until < dto.StartsAtUtc)
            return BadRequest("Repeat end date must be on or after the first date");

        var starts = ActivitySchedule.OccurrenceStarts(dto.StartsAtUtc, frequency, dto.RecurrenceUntilUtc);
        var groupId = starts.Count > 1 ? Guid.NewGuid() : (Guid?)null;
        var storedFrequency = starts.Count > 1 ? frequency : RecurrenceFrequency.None;
        var storedUntil = storedFrequency == RecurrenceFrequency.None ? null : dto.RecurrenceUntilUtc;

        Activity? first = null;
        foreach (var start in starts)
        {
            var activity = MapNew(dto, start, groupId, storedFrequency, storedUntil);
            foreach (var label in NormalizeTags(dto.Tags))
                activity.Tags.Add(new ActivityTag { Label = label });
            _db.Activities.Add(activity);
            var volunteerError = await AddVolunteerNeedsAsync(activity, dto);
            if (volunteerError is not null) return BadRequest(volunteerError);
            first ??= activity;
        }

        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = first!.Id }, first);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, ActivityDto dto)
    {
        var activity = await _db.Activities.FirstOrDefaultAsync(a => a.Id == id);
        if (activity is null || !activity.IsActive) return NotFound();

        if (activity.Kind == ActivityKind.PersonalActivity)
        {
            if (activity.CreatedByUserId != UserId) return Forbid();
        }
        else if (activity.ClubId.HasValue)
        {
            await _auth.EnsureClubAdminAsync(UserId, activity.ClubId.Value);
        }

        if (activity.Kind != ActivityKind.PersonalActivity && activity.StartsAtUtc < DateTime.UtcNow)
            return BadRequest("Past activities cannot be edited");

        Apply(activity, dto);
        await ReplaceTagsAsync(activity.Id, dto.Tags);
        var volunteerError = await AddVolunteerNeedsAsync(activity, dto);
        if (volunteerError is not null) return BadRequest(volunteerError);
        await _db.SaveChangesAsync();
        return Ok(activity);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var activity = await _db.Activities.FindAsync(id);
        if (activity is null) return NotFound();
        if (activity.Kind == ActivityKind.PersonalActivity && activity.CreatedByUserId != UserId) return Forbid();
        if (activity.ClubId.HasValue) await _auth.EnsureClubAdminAsync(UserId, activity.ClubId.Value);
        activity.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    public record AttendanceRequest(AttendanceStatus Status, string? PaceGroup);

    [HttpPost("{id:guid}/attendance")]
    public async Task<ActionResult> SetAttendance(Guid id, AttendanceRequest req)
    {
        var activity = await _db.Activities.FindAsync(id);
        if (activity is null || !activity.IsActive) return NotFound();
        if (activity.Kind == ActivityKind.PersonalActivity) return BadRequest();
        if (activity.ClubId.HasValue) await _auth.EnsureClubMemberAsync(UserId, activity.ClubId.Value);
        if (ActivitySchedule.HasEnded(activity.StartsAtUtc, activity.EndsAtUtc, DateTime.UtcNow))
            return BadRequest("This activity has ended");

        var attendance = await _db.ActivityAttendances.FirstOrDefaultAsync(a => a.ActivityId == id && a.UserId == UserId);
        if (attendance is null)
        {
            attendance = new ActivityAttendance { ActivityId = id, UserId = UserId };
            _db.ActivityAttendances.Add(attendance);
        }

        attendance.Status = req.Status;
        attendance.PaceGroup = req.PaceGroup;
        attendance.UpdatedAtUtc = DateTime.UtcNow;
        if (req.Status != AttendanceStatus.Going)
        {
            attendance.Attended = null;
            attendance.AttendedAtUtc = null;
        }
        await _db.SaveChangesAsync();
        return Ok(new
        {
            attendance.Status,
            IsGoing = attendance.Status == AttendanceStatus.Going,
            attendance.Attended
        });
    }

    public record ConfirmAttendanceRequest(bool Attended);

    [HttpPost("{id:guid}/attendance/confirm")]
    public async Task<ActionResult> ConfirmAttendance(Guid id, ConfirmAttendanceRequest req)
    {
        var activity = await _db.Activities.FindAsync(id);
        if (activity is null || !activity.IsActive) return NotFound();
        if (activity.Kind == ActivityKind.PersonalActivity) return BadRequest();
        if (activity.ClubId.HasValue) await _auth.EnsureClubMemberAsync(UserId, activity.ClubId.Value);
        if (!ActivitySchedule.HasEnded(activity.StartsAtUtc, activity.EndsAtUtc, DateTime.UtcNow))
            return BadRequest("This activity has not finished yet");

        var attendance = await _db.ActivityAttendances.FirstOrDefaultAsync(a => a.ActivityId == id && a.UserId == UserId);
        if (attendance is null)
        {
            attendance = new ActivityAttendance { ActivityId = id, UserId = UserId };
            _db.ActivityAttendances.Add(attendance);
        }

        var wasAttended = attendance.Attended;
        attendance.Attended = req.Attended;
        attendance.AttendedAtUtc = DateTime.UtcNow;
        attendance.UpdatedAtUtc = DateTime.UtcNow;

        var checkIn = await _db.ActivityCheckIns.FirstOrDefaultAsync(c => c.ActivityId == id && c.UserId == UserId);
        if (req.Attended)
        {
            if (checkIn is null)
            {
                _db.ActivityCheckIns.Add(new ActivityCheckIn
                {
                    ActivityId = id,
                    UserId = UserId,
                    PaceGroup = attendance.PaceGroup
                });
            }
        }
        else if (checkIn is not null)
        {
            _db.ActivityCheckIns.Remove(checkIn);
        }

        if (wasAttended != true && req.Attended)
        {
            var profile = await _db.MemberProfiles.FirstOrDefaultAsync(p => p.UserId == UserId);
            if (profile is not null) profile.ActivitiesCompleted++;
        }
        else if (wasAttended == true && !req.Attended)
        {
            var profile = await _db.MemberProfiles.FirstOrDefaultAsync(p => p.UserId == UserId);
            if (profile is not null && profile.ActivitiesCompleted > 0) profile.ActivitiesCompleted--;
        }

        await _db.SaveChangesAsync();
        return Ok(new
        {
            attendance.Status,
            IsGoing = attendance.Status == AttendanceStatus.Going,
            attendance.Attended,
            CheckedIn = req.Attended
        });
    }

    public class RatingRequest
    {
        public int OverallRating { get; set; }
        public string? Comments { get; set; }
    }

    [HttpPost("{id:guid}/ratings")]
    public async Task<ActionResult> Rate(Guid id, RatingRequest req)
    {
        if (req.OverallRating is < 1 or > 5)
            return BadRequest("Rating must be between 1 and 5");

        var activity = await RequireClubActivity(id);
        if (activity is null) return NotFound();

        var attendance = await _db.ActivityAttendances.FirstOrDefaultAsync(a => a.ActivityId == id && a.UserId == UserId);
        if (attendance is null || attendance.Attended != true)
            return BadRequest("Check in first by confirming you went");

        attendance.RatingSkipped = false;

        var existing = await _db.ActivityRatings.FirstOrDefaultAsync(r => r.ActivityId == id && r.UserId == UserId);
        if (existing is not null)
        {
            existing.OverallRating = req.OverallRating;
            existing.Enjoyment = req.OverallRating;
            existing.Comments = req.Comments;
            await _db.SaveChangesAsync();
            return Ok(new { existing.OverallRating, existing.Comments });
        }

        var rating = new ActivityRating
        {
            ActivityId = id,
            UserId = UserId,
            OverallRating = req.OverallRating,
            Enjoyment = req.OverallRating,
            PaceFeedback = "JustRight",
            RouteThumbsUp = req.OverallRating >= 4,
            WouldDoAgain = req.OverallRating >= 4,
            Comments = req.Comments
        };
        _db.ActivityRatings.Add(rating);
        await _db.SaveChangesAsync();
        return Ok(new { rating.OverallRating, rating.Comments });
    }

    [HttpPost("{id:guid}/ratings/skip")]
    public async Task<ActionResult> SkipRating(Guid id)
    {
        var activity = await RequireClubActivity(id);
        if (activity is null) return NotFound();
        var attendance = await _db.ActivityAttendances.FirstOrDefaultAsync(a => a.ActivityId == id && a.UserId == UserId);
        if (attendance is null || attendance.Attended != true)
            return BadRequest("Check in first by confirming you went");
        attendance.RatingSkipped = true;
        attendance.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { attendance.RatingSkipped });
    }

    public record TrainingParticipationRequest(
        ParticipationMode Mode,
        bool Completed,
        double? DistanceMiles,
        int? TimeMinutes,
        string? Effort,
        string? Notes);

    [HttpPost("{id:guid}/training-participation")]
    public async Task<ActionResult> TrainingParticipation(Guid id, TrainingParticipationRequest req)
    {
        var activity = await _db.Activities.FindAsync(id);
        if (activity is null || !activity.IsActive || !activity.IsTrainingSession) return BadRequest("Not a training session");
        if (activity.ClubId.HasValue) await _auth.EnsureClubMemberAsync(UserId, activity.ClubId.Value);
        if (req.Mode == ParticipationMode.Virtual && !activity.VirtualParticipationEnabled)
            return BadRequest("Virtual participation disabled");

        var part = await _db.TrainingParticipations.FirstOrDefaultAsync(p => p.ActivityId == id && p.UserId == UserId);
        var wasCompleted = part?.Completed == true;
        if (part is null)
        {
            part = new TrainingParticipation { ActivityId = id, UserId = UserId };
            _db.TrainingParticipations.Add(part);
        }

        part.Mode = req.Mode;
        part.Completed = req.Completed;
        part.DistanceMiles = req.DistanceMiles;
        part.TimeMinutes = req.TimeMinutes;
        part.Effort = req.Effort;
        part.Notes = req.Notes;
        part.UpdatedAtUtc = DateTime.UtcNow;

        if (req.Completed && !wasCompleted)
        {
            var profile = await _db.MemberProfiles.FirstOrDefaultAsync(p => p.UserId == UserId);
            if (profile is not null) profile.TrainingSessionsCompleted++;
        }

        await _db.SaveChangesAsync();

        var aggregates = await _db.TrainingParticipations
            .Where(p => p.ActivityId == id)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                InPerson = g.Count(x => x.Mode == ParticipationMode.InPerson),
                Virtual = g.Count(x => x.Mode == ParticipationMode.Virtual),
                Completed = g.Count(x => x.Completed)
            })
            .FirstOrDefaultAsync();

        return Ok(new { participation = part, aggregates });
    }

    private async Task<Activity?> RequireClubActivity(Guid id)
    {
        var activity = await _db.Activities.FindAsync(id);
        if (activity is null || !activity.IsActive || activity.Kind == ActivityKind.PersonalActivity) return null;
        if (activity.ClubId.HasValue) await _auth.EnsureClubMemberAsync(UserId, activity.ClubId.Value);
        return activity;
    }

    private Activity MapNew(
        ActivityDto dto,
        DateTime? startsAtUtc = null,
        Guid? recurrenceGroupId = null,
        RecurrenceFrequency recurrenceFrequency = RecurrenceFrequency.None,
        DateTime? recurrenceUntilUtc = null)
    {
        var activity = new Activity { CreatedByUserId = UserId };
        Apply(activity, dto);
        if (startsAtUtc.HasValue)
        {
            activity.StartsAtUtc = startsAtUtc.Value;
            if (dto.EndsAtUtc.HasValue)
                activity.EndsAtUtc = startsAtUtc.Value + (dto.EndsAtUtc.Value - dto.StartsAtUtc);
        }

        activity.RecurrenceGroupId = recurrenceGroupId;
        activity.RecurrenceFrequency = recurrenceFrequency;
        activity.RecurrenceUntilUtc = recurrenceUntilUtc;
        return activity;
    }

    private static void Apply(Activity activity, ActivityDto dto)
    {
        activity.ClubId = dto.Kind == ActivityKind.PersonalActivity ? dto.ClubId : dto.ClubId;
        activity.Kind = dto.Kind;
        activity.Title = dto.Title;
        activity.Description = dto.Description;
        activity.StartsAtUtc = AsUtc(dto.StartsAtUtc);
        activity.EndsAtUtc = AsUtc(dto.EndsAtUtc);
        activity.MeetingPoint = dto.MeetingPoint;
        activity.Location = dto.Location;
        activity.Route = dto.Route;
        activity.DistanceMiles = dto.DistanceMiles;
        activity.PaceGroups = dto.PaceGroups;
        activity.RunType = dto.RunType;
        activity.RunLeaderUserId = dto.RunLeaderUserId;
        activity.BackMarkerUserId = dto.BackMarkerUserId;
        activity.MaxCapacity = dto.MaxCapacity;
        activity.IsTrainingSession = dto.Kind == ActivityKind.ClubActivity && dto.IsTrainingSession;
        activity.SessionType = activity.IsTrainingSession ? dto.SessionType : null;
        activity.WorkoutInstructions = activity.IsTrainingSession ? dto.WorkoutInstructions : null;
        activity.TargetPaceOrEffort = activity.IsTrainingSession ? dto.TargetPaceOrEffort : null;
        activity.CoachUserId = activity.IsTrainingSession ? dto.CoachUserId : null;
        activity.VirtualParticipationEnabled = activity.IsTrainingSession && dto.VirtualParticipationEnabled;
    }

    private static DateTime AsUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static DateTime? AsUtc(DateTime? value)
        => value is { } date ? AsUtc(date) : null;

    private static List<string> NormalizeTags(IReadOnlyList<string>? tags)
    {
        var labels = new List<string>();
        if (tags is null) return labels;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in tags)
        {
            var label = raw?.Trim();
            if (string.IsNullOrWhiteSpace(label)) continue;
            if (label.Length > 40) label = label[..40];
            if (!seen.Add(label)) continue;
            labels.Add(label);
            if (labels.Count == 12) break;
        }
        return labels;
    }

    private async Task ReplaceTagsAsync(Guid activityId, IReadOnlyList<string>? tags)
    {
        if (tags is null) return;

        var labels = NormalizeTags(tags);
        var existing = await _db.ActivityTags.Where(t => t.ActivityId == activityId).ToListAsync();
        if (existing.Count > 0)
            _db.ActivityTags.RemoveRange(existing);

        foreach (var label in labels)
        {
            var tag = new ActivityTag
            {
                Id = Guid.NewGuid(),
                ActivityId = activityId,
                Label = label
            };
            _db.Entry(tag).State = EntityState.Added;
        }
    }

    private async Task<string?> AddVolunteerNeedsAsync(Activity activity, ActivityDto dto)
    {
        if (dto.VolunteerNeeds is not { Count: > 0 }) return null;
        if (activity.Kind is not (ActivityKind.ClubActivity or ActivityKind.Race) || !activity.ClubId.HasValue)
            return "Volunteer slots only on club activities and races";

        var typeIds = dto.VolunteerNeeds.Select(n => n.VolunteerRoleTypeId).Distinct().ToList();
        var types = await _db.VolunteerRoleTypes
            .Where(t => t.ClubId == activity.ClubId && t.IsActive && typeIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id);

        foreach (var need in dto.VolunteerNeeds)
        {
            if (need.Count < 1) continue;
            if (need.Count > 20) return "At most 20 volunteers per type";
            if (!types.TryGetValue(need.VolunteerRoleTypeId, out var roleType))
                return "Unknown or inactive volunteer type";

            var tag = string.IsNullOrWhiteSpace(need.Tag) ? null : need.Tag.Trim();
            for (var i = 0; i < need.Count; i++)
            {
                _db.VolunteerSlots.Add(new VolunteerSlot
                {
                    ActivityId = activity.Id,
                    Role = roleType.Name,
                    Tag = tag,
                    Description = roleType.Description
                });
            }
        }

        return null;
    }
}
