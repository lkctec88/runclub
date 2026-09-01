using RunClub.Domain;

namespace RunClub.Application.Abstractions;

public interface IClubAuthorizationService
{
    Task<bool> IsSuperAdminAsync(string userId);
    Task<bool> IsClubAdminAsync(string userId, Guid clubId);
    Task<bool> IsClubMemberAsync(string userId, Guid clubId);
    Task EnsureClubMemberAsync(string userId, Guid clubId);
    Task EnsureClubAdminAsync(string userId, Guid clubId);
    Task EnsureSuperAdminAsync(string userId);
}

public interface IJwtTokenService
{
    Task<string> CreateTokenAsync(string userId, string email, PlatformRole platformRole, IEnumerable<(Guid ClubId, ClubRole Role)> memberships);
}

public interface ICsvMemberService
{
    Task<CsvImportResult> ImportAsync(
        Guid clubId,
        Stream csvStream,
        bool dryRun,
        bool fullRoster = false,
        string? actingUserId = null,
        CancellationToken ct = default);
    Task<CsvImportResult> BulkDeleteAsync(Guid clubId, Stream csvStream, bool dryRun, CancellationToken ct = default);
    string GetTemplateCsv();
}

public record CsvImportRowResult(int Row, string Identifier, string Action, string? Error = null);
public record CsvImportResult(
    bool DryRun,
    int Added,
    int Updated,
    int Removed,
    int Skipped,
    IReadOnlyList<CsvImportRowResult> Rows,
    int Reactivated = 0,
    bool FullRoster = false);

public interface IIcsCalendarService
{
    Task<string> BuildPersonalFeedAsync(string userId, CancellationToken ct = default);
    Task<string> BuildActivityEventAsync(Guid activityId, CancellationToken ct = default);
}
