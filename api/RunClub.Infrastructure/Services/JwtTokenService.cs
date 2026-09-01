using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RunClub.Application.Abstractions;
using RunClub.Domain;

namespace RunClub.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config) => _config = config;

    public Task<string> CreateTokenAsync(
        string userId,
        string email,
        PlatformRole platformRole,
        IEnumerable<(Guid ClubId, ClubRole Role)> memberships)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _config["Jwt:Key"] ?? "RunClubDevSuperSecretKey_ChangeMe_32chars!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.NameIdentifier, userId),
            new("platform_role", platformRole.ToString())
        };

        if (platformRole == PlatformRole.SuperAdmin || memberships.Any(m => m.Role == ClubRole.SuperAdmin))
            claims.Add(new Claim(ClaimTypes.Role, "SuperAdmin"));

        foreach (var (clubId, role) in memberships)
            claims.Add(new Claim("club", $"{clubId}:{role}"));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "runclub",
            audience: _config["Jwt:Audience"] ?? "runclub",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
    }
}
