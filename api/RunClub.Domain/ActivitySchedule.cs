namespace RunClub.Domain;

public static class ActivitySchedule
{
    public static TimeSpan DefaultDuration(ActivityKind kind)
        => kind == ActivityKind.Race ? TimeSpan.FromHours(2) : TimeSpan.FromHours(1);

    public static DateTime EffectiveEndUtc(DateTime startsAtUtc, DateTime? endsAtUtc, ActivityKind kind)
        => endsAtUtc ?? startsAtUtc.Add(DefaultDuration(kind));

    public static bool HasEnded(DateTime startsAtUtc, DateTime? endsAtUtc, DateTime utcNow, ActivityKind kind)
        => utcNow >= EffectiveEndUtc(startsAtUtc, endsAtUtc, kind);

    public const int MaxOccurrences = 52;
    public const int DefaultOccurrenceCount = 12;

    public static bool CanRecur(ActivityKind kind)
        => kind == ActivityKind.ClubActivity;

    public static IReadOnlyList<DateTime> OccurrenceStarts(
        DateTime firstUtc,
        RecurrenceFrequency frequency,
        DateTime? untilUtc)
    {
        var dates = new List<DateTime> { firstUtc };
        if (frequency is not (RecurrenceFrequency.Weekly or RecurrenceFrequency.Monthly))
            return dates;

        var limit = untilUtc ?? (frequency == RecurrenceFrequency.Weekly
            ? firstUtc.AddDays(7 * (DefaultOccurrenceCount - 1))
            : firstUtc.AddMonths(DefaultOccurrenceCount - 1));

        var cursor = firstUtc;
        while (dates.Count < MaxOccurrences)
        {
            cursor = frequency == RecurrenceFrequency.Weekly ? cursor.AddDays(7) : cursor.AddMonths(1);
            if (cursor > limit) break;
            dates.Add(cursor);
        }

        return dates;
    }
}
