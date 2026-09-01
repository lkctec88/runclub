using System.Text.RegularExpressions;

namespace RunClub.Domain;

public static class MembershipIdentity
{
    public static string NormalizeEnglandAthleticsNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return Regex.Replace(value, @"[^A-Za-z0-9]", "").ToUpperInvariant();
    }

    public static string NormalizeLastName(string? value)
        => (value ?? string.Empty).Trim();

    public static bool LastNamesMatch(string? left, string? right)
        => string.Equals(NormalizeLastName(left), NormalizeLastName(right), StringComparison.OrdinalIgnoreCase);
}
