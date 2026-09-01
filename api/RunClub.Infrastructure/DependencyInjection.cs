using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RunClub.Application.Abstractions;
using RunClub.Infrastructure.Identity;
using RunClub.Infrastructure.Persistence;
using RunClub.Infrastructure.Services;

namespace RunClub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? "Host=localhost;Port=5432;Database=runclub;Username=runclub;Password=runclub";

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IClubAuthorizationService, ClubAuthorizationService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ICsvMemberService, CsvMemberService>();
        services.AddScoped<IIcsCalendarService, IcsCalendarService>();

        return services;
    }
}
