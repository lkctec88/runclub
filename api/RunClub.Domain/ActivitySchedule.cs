namespace RunClub.Domain;

public static class ActivitySchedule
{
    public static readonly TimeSpan DefaultDuration = TimeSpan.FromHours(2);

    public static DateTime EffectiveEndUtc(DateTime startsAtUtc, DateTime? endsAtUtc)
        => endsAtUtc ?? startsAtUtc.Add(DefaultDuration);

    public static bool HasEnded(DateTime startsAtUtc, DateTime? endsAtUtc, DateTime utcNow)
        => utcNow >= EffectiveEndUtc(startsAtUtc, endsAtUtc);
}
