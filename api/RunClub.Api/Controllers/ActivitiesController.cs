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
        bool VirtualParticipationEnabled);

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

        var activity = MapNew(dto);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = activity.Id }, activity);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, ActivityDto dto)
    {
        var activity = await _db.Activities.FindAsync(id);
        if (activity is null || !activity.IsActive) return NotFound();

        if (activity.Kind == ActivityKind.PersonalActivity)
        {
            if (activity.CreatedByUserId != UserId) return Forbid();
        }
        else if (activity.ClubId.HasValue)
        {
            await _auth.EnsureClubAdminAsync(UserId, activity.ClubId.Value);
        }

        Apply(activity, dto);
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

    private Activity MapNew(ActivityDto dto)
    {
        var activity = new Activity { CreatedByUserId = UserId };
        Apply(activity, dto);
        return activity;
    }

    private static void Apply(Activity activity, ActivityDto dto)
    {
        activity.ClubId = dto.Kind == ActivityKind.PersonalActivity ? dto.ClubId : dto.ClubId;
        activity.Kind = dto.Kind;
        activity.Title = dto.Title;
        activity.Description = dto.Description;
        activity.StartsAtUtc = dto.StartsAtUtc;
        activity.EndsAtUtc = dto.EndsAtUtc;
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
}
