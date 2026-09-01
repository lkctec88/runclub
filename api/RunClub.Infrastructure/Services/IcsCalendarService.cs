using System.Text;
using Microsoft.EntityFrameworkCore;
using RunClub.Application.Abstractions;
using RunClub.Domain;
using RunClub.Infrastructure.Persistence;

namespace RunClub.Infrastructure.Services;

public class IcsCalendarService : IIcsCalendarService
{
    private readonly AppDbContext _db;

    public IcsCalendarService(AppDbContext db) => _db = db;

    public async Task<string> BuildPersonalFeedAsync(string userId, CancellationToken ct = default)
    {
        var going = await _db.ActivityAttendances
            .Include(a => a.Activity)
            .Where(a => a.UserId == userId && a.Status == AttendanceStatus.Going && a.Activity.IsActive)
            .Select(a => a.Activity)
            .ToListAsync(ct);

        var virtualRuns = await _db.TrainingParticipations
            .Include(p => p.Activity)
            .Where(p => p.UserId == userId && p.Mode == ParticipationMode.Virtual && p.Activity.IsActive)
            .Select(p => p.Activity)
            .ToListAsync(ct);

        var volunteered = await _db.VolunteerSlots
            .Include(s => s.Activity)
            .Where(s => s.AssignedUserId == userId && s.Status != VolunteerSlotStatus.Available && s.Activity.IsActive)
            .Select(s => s.Activity)
            .ToListAsync(ct);

        var personal = await _db.Activities
            .Where(r => r.Kind == ActivityKind.PersonalActivity && r.CreatedByUserId == userId && r.IsActive)
            .ToListAsync(ct);

        var activities = going.Concat(virtualRuns).Concat(volunteered).Concat(personal)
            .GroupBy(r => r.Id)
            .Select(g => g.First())
            .OrderBy(r => r.StartsAtUtc)
            .ToList();

        return BuildCalendar("RunClub Personal", activities);
    }

    public async Task<string> BuildActivityEventAsync(Guid activityId, CancellationToken ct = default)
    {
        var activity = await _db.Activities.FirstOrDefaultAsync(r => r.Id == activityId && r.IsActive, ct)
            ?? throw new InvalidOperationException("Activity not found.");
        return BuildCalendar(activity.Title, [activity]);
    }

    private static string BuildCalendar(string name, IEnumerable<Domain.Entities.Activity> activities)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//RunClub//EN");
        sb.AppendLine($"X-WR-CALNAME:{Escape(name)}");
        foreach (var activity in activities)
        {
            var end = activity.EndsAtUtc ?? activity.StartsAtUtc.AddHours(1);
            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:{activity.Id}@runclub");
            sb.AppendLine($"DTSTAMP:{Format(DateTime.UtcNow)}");
            sb.AppendLine($"DTSTART:{Format(activity.StartsAtUtc)}");
            sb.AppendLine($"DTEND:{Format(end)}");
            sb.AppendLine($"SUMMARY:{Escape(activity.Title)}");
            var loc = activity.MeetingPoint ?? activity.Location ?? "";
            if (!string.IsNullOrWhiteSpace(loc))
                sb.AppendLine($"LOCATION:{Escape(loc)}");
            if (!string.IsNullOrWhiteSpace(activity.Description))
                sb.AppendLine($"DESCRIPTION:{Escape(activity.Description)}");
            sb.AppendLine("END:VEVENT");
        }

        sb.AppendLine("END:VCALENDAR");
        return sb.ToString();
    }

    private static string Format(DateTime dt) => dt.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'");
    private static string Escape(string value) =>
        value.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\n", "\\n");
}
