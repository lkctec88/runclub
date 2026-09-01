using System.Text;
using Microsoft.EntityFrameworkCore;
using RunClub.Application.Abstractions;
using RunClub.Domain;
using RunClub.Domain.Entities;
using RunClub.Infrastructure.Identity;
using RunClub.Infrastructure.Persistence;

namespace RunClub.Infrastructure.Services;

public class CsvMemberService : ICsvMemberService
{
    private readonly AppDbContext _db;

    public CsvMemberService(AppDbContext db) => _db = db;

    public string GetTemplateCsv() =>
        "firstName,lastName,englandAthleticsNumber,role,status\r\nJane,Doe,1234567,Member,Active\r\nAlex,Admin,7654321,Admin,Active\r\nSam,Runner,9999999,Member,Lapsed\r\n";

    public async Task<CsvImportResult> ImportAsync(
        Guid clubId,
        Stream csvStream,
        bool dryRun,
        bool fullRoster = false,
        string? actingUserId = null,
        CancellationToken ct = default)
    {
        _ = await _db.Clubs.FindAsync([clubId], ct)
            ?? throw new InvalidOperationException("Club not found.");

        var rows = ParseRows(csvStream);
        var results = new List<CsvImportRowResult>();
        var added = 0;
        var updated = 0;
        var reactivated = 0;
        var lapsed = 0;
        var skipped = 0;
        var seenEa = new HashSet<string>(StringComparer.Ordinal);

        var invites = await _db.ValidateMembers.Where(x => x.ClubId == clubId).ToListAsync(ct);
        var memberships = await _db.ClubMemberships.Where(m => m.ClubId == clubId).ToListAsync(ct);
        var userIds = memberships.Select(m => m.UserId)
            .Concat(invites.Where(i => i.ClaimedUserId is not null).Select(i => i.ClaimedUserId!))
            .Distinct()
            .ToList();
        var profiles = await _db.MemberProfiles.Where(p => userIds.Contains(p.UserId)).ToListAsync(ct);
        var users = await _db.Users.Where(u => userIds.Contains(u.Id)).ToListAsync(ct);

        for (var i = 0; i < rows.Count; i++)
        {
            var rowNum = i + 2;
            var (firstName, lastName, eaRaw, roleRaw, statusRaw) = rows[i];
            var identifier = $"{lastName} / {eaRaw}";

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(eaRaw))
            {
                results.Add(new CsvImportRowResult(rowNum, identifier, "skipped", "First name, last name, and England Athletics number are required"));
                skipped++;
                continue;
            }

            var ea = MembershipIdentity.NormalizeEnglandAthleticsNumber(eaRaw);
            if (ea.Length == 0)
            {
                results.Add(new CsvImportRowResult(rowNum, identifier, "skipped", "England Athletics number is invalid"));
                skipped++;
                continue;
            }

            if (!seenEa.Add(ea))
            {
                results.Add(new CsvImportRowResult(rowNum, identifier, "skipped", "Duplicate England Athletics number in this file"));
                skipped++;
                continue;
            }

            var role = ParseClubRole(roleRaw);
            var active = ParseActive(statusRaw);
            if (active is null)
            {
                results.Add(new CsvImportRowResult(rowNum, identifier, "skipped", "Status must be Active or Lapsed"));
                skipped++;
                continue;
            }

            var invite = FindInvite(invites, ea);
            var profile = FindProfile(profiles, invite, ea);
            if (profile is not null && !MembershipIdentity.LastNamesMatch(profile.LastName, lastName))
            {
                results.Add(new CsvImportRowResult(rowNum, identifier, "skipped", "Last name does not match the existing account"));
                skipped++;
                continue;
            }

            if (profile is null && invite is not null && invite.ClaimedUserId is null
                && !MembershipIdentity.LastNamesMatch(invite.LastName, lastName)
                && invite.IsActive)
            {
                // Unclaimed row with a different last name: treat as an update to the register list.
            }

            if (active.Value)
            {
                var wasInactive = (invite is not null && !invite.IsActive)
                    || (invite?.ClaimedUserId is not null
                        && memberships.Any(m => m.UserId == invite.ClaimedUserId && m.ClubId == clubId && !m.IsActive));

                if (!dryRun)
                {
                    invite = Activate(
                        clubId,
                        invites,
                        memberships,
                        profiles,
                        users,
                        invite,
                        firstName.Trim(),
                        lastName.Trim(),
                        ea,
                        role);
                }

                if (invite is null && dryRun && wasInactive)
                {
                    results.Add(new CsvImportRowResult(rowNum, identifier, "would_reactivate"));
                    reactivated++;
                }
                else if (wasInactive)
                {
                    results.Add(new CsvImportRowResult(rowNum, identifier, dryRun ? "would_reactivate" : "reactivated"));
                    reactivated++;
                }
                else if (invite is null)
                {
                    results.Add(new CsvImportRowResult(rowNum, identifier, dryRun ? "would_add" : "added"));
                    added++;
                }
                else
                {
                    results.Add(new CsvImportRowResult(rowNum, identifier, dryRun ? "would_update" : "updated"));
                    updated++;
                }
            }
            else
            {
                if (invite is null && profile is null)
                {
                    results.Add(new CsvImportRowResult(rowNum, identifier, "skipped", "Not found — cannot lapse an unknown member"));
                    skipped++;
                    continue;
                }

                if (!dryRun)
                    Lapse(invites, memberships, invite, profile?.UserId);

                results.Add(new CsvImportRowResult(rowNum, identifier, dryRun ? "would_lapse" : "lapsed"));
                lapsed++;
            }
        }

        if (fullRoster)
        {
            foreach (var invite in invites.Where(i => i.IsActive && !seenEa.Contains(i.EnglandAthleticsNumber)).ToList())
            {
                if (invite.Role == ClubRole.SuperAdmin)
                {
                    results.Add(new CsvImportRowResult(0, $"{invite.LastName} / {invite.EnglandAthleticsNumber}", "skipped", "SuperAdmin left active (not in full roster file)"));
                    skipped++;
                    continue;
                }

                if (invite.ClaimedUserId is not null && invite.ClaimedUserId == actingUserId)
                {
                    results.Add(new CsvImportRowResult(0, $"{invite.LastName} / {invite.EnglandAthleticsNumber}", "skipped", "Your own membership was left active"));
                    skipped++;
                    continue;
                }

                if (!dryRun)
                    Lapse(invites, memberships, invite, invite.ClaimedUserId);

                results.Add(new CsvImportRowResult(0, $"{invite.LastName} / {invite.EnglandAthleticsNumber}", dryRun ? "would_lapse" : "lapsed"));
                lapsed++;
            }

            foreach (var membership in memberships.Where(m => m.IsActive).ToList())
            {
                var profile = profiles.FirstOrDefault(p => p.UserId == membership.UserId);
                var ea = MembershipIdentity.NormalizeEnglandAthleticsNumber(profile?.EnglandAthleticsNumber);
                if (ea.Length > 0 && seenEa.Contains(ea)) continue;
                if (invites.Any(i => i.ClaimedUserId == membership.UserId)) continue;
                if (membership.Role == ClubRole.SuperAdmin || membership.UserId == actingUserId)
                {
                    skipped++;
                    continue;
                }

                if (!dryRun) membership.IsActive = false;
                results.Add(new CsvImportRowResult(0, profile is null ? membership.UserId : $"{profile.LastName} / {ea}", dryRun ? "would_lapse" : "lapsed"));
                lapsed++;
            }
        }

        if (!dryRun) await _db.SaveChangesAsync(ct);
        return new CsvImportResult(dryRun, added, updated, lapsed, skipped, results, reactivated, fullRoster);
    }

    public async Task<CsvImportResult> BulkDeleteAsync(Guid clubId, Stream csvStream, bool dryRun, CancellationToken ct = default)
    {
        _ = await _db.Clubs.FindAsync([clubId], ct)
            ?? throw new InvalidOperationException("Club not found.");

        var numbers = ParseDeleteNumbers(csvStream);
        var results = new List<CsvImportRowResult>();
        var lapsed = 0;
        var skipped = 0;

        var invites = await _db.ValidateMembers.Where(x => x.ClubId == clubId).ToListAsync(ct);
        var memberships = await _db.ClubMemberships.Where(m => m.ClubId == clubId).ToListAsync(ct);
        var userIds = memberships.Select(m => m.UserId).Distinct().ToList();
        var profiles = await _db.MemberProfiles.Where(p => userIds.Contains(p.UserId)).ToListAsync(ct);

        for (var i = 0; i < numbers.Count; i++)
        {
            var rowNum = i + 2;
            var ea = MembershipIdentity.NormalizeEnglandAthleticsNumber(numbers[i]);
            if (ea.Length == 0)
            {
                results.Add(new CsvImportRowResult(rowNum, numbers[i], "skipped", "England Athletics number required"));
                skipped++;
                continue;
            }

            var invite = FindInvite(invites, ea);
            var profile = FindProfile(profiles, invite, ea);
            if (invite is null && profile is null)
            {
                results.Add(new CsvImportRowResult(rowNum, ea, "skipped", "Not found"));
                skipped++;
                continue;
            }

            if (!dryRun)
                Lapse(invites, memberships, invite, profile?.UserId ?? invite?.ClaimedUserId);

            results.Add(new CsvImportRowResult(rowNum, ea, dryRun ? "would_lapse" : "lapsed"));
            lapsed++;
        }

        if (!dryRun) await _db.SaveChangesAsync(ct);
        return new CsvImportResult(dryRun, 0, 0, lapsed, skipped, results);
    }

    private static ValidateMember? FindInvite(IEnumerable<ValidateMember> invites, string ea)
        => invites
            .Where(x => x.EnglandAthleticsNumber == ea)
            .OrderByDescending(x => x.ClaimedUserId is not null)
            .ThenByDescending(x => x.IsActive)
            .ThenByDescending(x => x.CreatedAtUtc)
            .FirstOrDefault();

    private static MemberProfile? FindProfile(List<MemberProfile> profiles, ValidateMember? invite, string ea)
    {
        if (invite?.ClaimedUserId is not null)
            return profiles.FirstOrDefault(p => p.UserId == invite.ClaimedUserId);
        return profiles.FirstOrDefault(p =>
            MembershipIdentity.NormalizeEnglandAthleticsNumber(p.EnglandAthleticsNumber) == ea);
    }

    private ValidateMember Activate(
        Guid clubId,
        List<ValidateMember> invites,
        List<ClubMembership> memberships,
        List<MemberProfile> profiles,
        List<AppUser> users,
        ValidateMember? invite,
        string firstName,
        string lastName,
        string ea,
        ClubRole role)
    {
        if (invite is null)
        {
            invite = new ValidateMember
            {
                ClubId = clubId,
                FirstName = firstName,
                LastName = lastName,
                EnglandAthleticsNumber = ea,
                Role = role,
                IsActive = true
            };
            _db.ValidateMembers.Add(invite);
            invites.Add(invite);
        }
        else
        {
            invite.FirstName = firstName;
            invite.LastName = lastName;
            invite.Role = role;
            invite.IsActive = true;
        }

        var userId = invite.ClaimedUserId;
        if (userId is null) return invite;

        var membership = memberships.FirstOrDefault(m => m.UserId == userId && m.ClubId == clubId);
        if (membership is null)
        {
            membership = new ClubMembership
            {
                UserId = userId,
                ClubId = clubId,
                Role = role,
                IsActive = true
            };
            _db.ClubMemberships.Add(membership);
            memberships.Add(membership);
        }
        else
        {
            membership.IsActive = true;
            membership.Role = role;
        }

        var profile = profiles.FirstOrDefault(p => p.UserId == userId);
        if (profile is not null)
        {
            profile.FirstName = firstName;
            profile.LastName = lastName;
            profile.EnglandAthleticsNumber = ea;
        }

        var user = users.FirstOrDefault(u => u.Id == userId);
        if (user is not null)
            SyncPlatformRole(user, userId, clubId, role, memberships);

        return invite;
    }

    private static void Lapse(
        List<ValidateMember> invites,
        List<ClubMembership> memberships,
        ValidateMember? invite,
        string? userId)
    {
        if (invite is not null)
            invite.IsActive = false;

        var uid = userId ?? invite?.ClaimedUserId;
        if (uid is null) return;

        ClubMembership? membership;
        if (invite is not null)
            membership = memberships.FirstOrDefault(m => m.UserId == uid && m.ClubId == invite.ClubId);
        else
            membership = memberships.FirstOrDefault(m => m.UserId == uid);

        if (membership is not null)
            membership.IsActive = false;
    }

    private void SyncPlatformRole(AppUser user, string userId, Guid clubId, ClubRole role, List<ClubMembership> memberships)
    {
        var keepSuperAdmin = role == ClubRole.SuperAdmin
            || memberships.Any(m => m.UserId == userId && m.IsActive && m.ClubId != clubId && m.Role == ClubRole.SuperAdmin);
        user.PlatformRole = keepSuperAdmin ? PlatformRole.SuperAdmin : PlatformRole.User;
    }

    private static List<(string FirstName, string LastName, string EaNumber, string? Role, string? Status)> ParseRows(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var lines = new List<string>();
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (line is not null) lines.Add(line);
        }

        if (lines.Count == 0) return [];
        var start = lines[0].Contains("firstName", StringComparison.OrdinalIgnoreCase)
                    || lines[0].Contains("englandAthletics", StringComparison.OrdinalIgnoreCase)
            ? 1
            : 0;
        var result = new List<(string, string, string, string?, string?)>();
        for (var i = start; i < lines.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var parts = SplitCsv(lines[i]);
            result.Add((
                parts.ElementAtOrDefault(0) ?? "",
                parts.ElementAtOrDefault(1) ?? "",
                parts.ElementAtOrDefault(2) ?? "",
                parts.ElementAtOrDefault(3),
                parts.ElementAtOrDefault(4)));
        }

        return result;
    }

    private static List<string> ParseDeleteNumbers(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var lines = new List<string>();
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (line is not null) lines.Add(line);
        }

        if (lines.Count == 0) return [];
        var start = lines[0].Contains("englandAthletics", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        return lines.Skip(start)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => SplitCsv(l).ElementAtOrDefault(2) ?? SplitCsv(l).FirstOrDefault() ?? "")
            .ToList();
    }

    private static List<string> SplitCsv(string line)
        => line.Split(',').Select(p => p.Trim().Trim('"')).ToList();

    private static ClubRole ParseClubRole(string? roleRaw)
    {
        if (string.Equals(roleRaw, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            return ClubRole.SuperAdmin;
        if (string.Equals(roleRaw, "Admin", StringComparison.OrdinalIgnoreCase))
            return ClubRole.Admin;
        return ClubRole.Member;
    }

    private static bool? ParseActive(string? statusRaw)
    {
        if (string.IsNullOrWhiteSpace(statusRaw)) return true;
        if (statusRaw.Equals("Active", StringComparison.OrdinalIgnoreCase)
            || statusRaw.Equals("Paid", StringComparison.OrdinalIgnoreCase)
            || statusRaw.Equals("Yes", StringComparison.OrdinalIgnoreCase))
            return true;
        if (statusRaw.Equals("Lapsed", StringComparison.OrdinalIgnoreCase)
            || statusRaw.Equals("Inactive", StringComparison.OrdinalIgnoreCase)
            || statusRaw.Equals("Unpaid", StringComparison.OrdinalIgnoreCase)
            || statusRaw.Equals("No", StringComparison.OrdinalIgnoreCase))
            return false;
        return null;
    }
}
