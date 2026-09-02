using System.Security.Claims;
using System.Text;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RunClub.Domain;
using RunClub.Domain.Entities;
using RunClub.Infrastructure;
using RunClub.Infrastructure.Identity;
using RunClub.Infrastructure.Persistence;
using RunClub.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddInfrastructure(builder.Configuration);

if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor();
}

var jwtKey = builder.Configuration["Jwt:Key"] ?? "";
if (!builder.Environment.IsDevelopment() &&
    (jwtKey.Length < 32 || jwtKey.Contains("ChangeMe", StringComparison.Ordinal)))
{
    throw new InvalidOperationException("Jwt:Key must be a 32+ character secret in non-development environments.");
}
if (string.IsNullOrWhiteSpace(jwtKey))
{
    jwtKey = "RunClubDevSuperSecretKey_ChangeMe_32chars!";
}
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "runclub",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "runclub",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                context.Token = accessToken;
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdmin", p => p.RequireRole("SuperAdmin"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("web", p =>
        p.WithOrigins(
                builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                ?? ["http://localhost:5173", "http://localhost:4173"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var applyMigrations = app.Configuration.GetValue("Database:ApplyMigrations", true);
    if (applyMigrations)
    {
        await db.Database.MigrateAsync();
    }
    var seedEnabled = app.Configuration.GetValue<bool>("Seed:Enabled", app.Environment.IsDevelopment());
    if (seedEnabled)
    {
        await SeedData.EnsureSeededAsync(scope.ServiceProvider);
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ClubChatHub>("/hubs/club-chat");
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapGet("/ready", () => Results.Ok(new { status = "ready" })).AllowAnonymous();

app.Run();

public static class SeedData
{
    public static async Task EnsureSeededAsync(IServiceProvider sp)
    {
        var users = sp.GetRequiredService<UserManager<AppUser>>();
        var db = sp.GetRequiredService<AppDbContext>();

        var superEmail = "superadmin@runclub.local";
        var super = await users.FindByEmailAsync(superEmail);
        if (super is null)
        {
            super = new AppUser
            {
                UserName = superEmail,
                Email = superEmail,
                EmailConfirmed = true,
                PlatformRole = PlatformRole.SuperAdmin
            };
            await users.CreateAsync(super, "SuperAdmin123!");
            db.MemberProfiles.Add(new MemberProfile
            {
                UserId = super.Id,
                FirstName = "Super",
                LastName = "Admin"
            });
        }

        if (!await db.Clubs.AnyAsync())
        {
            var club = new Club
            {
                Name = "Holme Pierrepont Running Club",
                Description = "Holme Pierrepont Running Club",
                Location = "Holme Pierrepont, Nottingham",
                LogoUrl = "/clubs/hprc-logo.png"
            };
            db.Clubs.Add(club);

            var adminEmail = "admin@runclub.local";
            var admin = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            await users.CreateAsync(admin, "Admin123!");
            db.MemberProfiles.Add(new MemberProfile
            {
                UserId = admin.Id,
                FirstName = "Alex",
                LastName = "Admin",
                TypicalPace = "8:30-9:00/mile"
            });
            db.ClubMemberships.Add(new ClubMembership
            {
                UserId = admin.Id,
                ClubId = club.Id,
                Role = ClubRole.Admin
            });

            var memberEmail = "member@runclub.local";
            var member = new AppUser
            {
                UserName = memberEmail,
                Email = memberEmail,
                EmailConfirmed = true
            };
            await users.CreateAsync(member, "Member123!");
            db.MemberProfiles.Add(new MemberProfile
            {
                UserId = member.Id,
                FirstName = "Sam",
                LastName = "Runner",
                TypicalPace = "9:00-9:30/mile"
            });
            db.ClubMemberships.Add(new ClubMembership
            {
                UserId = member.Id,
                ClubId = club.Id,
                Role = ClubRole.Member
            });

            if (super is not null)
            {
                db.ClubMemberships.Add(new ClubMembership
                {
                    UserId = super.Id,
                    ClubId = club.Id,
                    Role = ClubRole.SuperAdmin
                });
            }

            db.ValidateMembers.Add(new ValidateMember
            {
                ClubId = club.Id,
                FirstName = "Jane",
                LastName = "Doe",
                EnglandAthleticsNumber = "1234567",
                Role = ClubRole.Member
            });

            AddDefaultVolunteerRoleTypes(db, club.Id);

            db.Activities.Add(new Activity
            {
                Kind = ActivityKind.ClubActivity,
                ClubId = club.Id,
                CreatedByUserId = admin.Id,
                Title = "Wednesday Club Run",
                Description = "Mixed pace social activity",
                StartsAtUtc = DateTime.UtcNow.Date.AddDays(2).AddHours(19),
                MeetingPoint = "Clubhouse",
                Location = "Holme Pierrepont National Watersports Centre",
                DistanceMiles = "5",
                PaceGroups = "Mixed",
                RunLeaderUserId = admin.Id,
                VolunteerSlots =
                [
                    new VolunteerSlot { Role = "Timekeeper", Description = "Record finish times at the clubhouse" },
                    new VolunteerSlot { Role = "Run lead", Tag = "8:30min/mi", Description = "Lead the 8:30 pace group" },
                    new VolunteerSlot { Role = "Tail runner", Description = "Activity at the back of the group" },
                    new VolunteerSlot { Role = "Setup", Description = "Help set up the start area" }
                ]
            });

            db.Activities.Add(new Activity
            {
                Kind = ActivityKind.ClubActivity,
                ClubId = club.Id,
                CreatedByUserId = admin.Id,
                Title = "Tuesday Hills",
                IsTrainingSession = true,
                SessionType = TrainingSessionType.Hills,
                WorkoutInstructions = "10 min warm-up\n6 x hill reps\n10 min cool-down",
                VirtualParticipationEnabled = true,
                StartsAtUtc = DateTime.UtcNow.Date.AddDays(1).AddHours(18),
                MeetingPoint = "Park Hill",
                Location = "Holme Pierrepont, Nottingham",
                DistanceMiles = "4",
                TargetPaceOrEffort = "Hard effort"
            });

            db.Activities.Add(new Activity
            {
                Kind = ActivityKind.Race,
                ClubId = club.Id,
                CreatedByUserId = admin.Id,
                Title = "Black Rocks Fell Race",
                Description = "Challenging fell race over rocky terrain with Peak District views",
                StartsAtUtc = new DateTime(2026, 9, 2, 18, 0, 0, DateTimeKind.Utc),
                MeetingPoint = "Black Rocks car park",
                Location = "Black Rocks, Peak District",
                DistanceMiles = "5.5",
                RunType = "Fell race",
                VolunteerSlots =
                [
                    new VolunteerSlot { Role = "Marshal", Description = "Station at a key point on the course" },
                    new VolunteerSlot { Role = "Registration", Description = "Sign runners in before the start" }
                ]
            });

            var pastRun = new Activity
            {
                Kind = ActivityKind.ClubActivity,
                ClubId = club.Id,
                CreatedByUserId = admin.Id,
                Title = "Sunday social run",
                Description = "Easy recovery run around the lake",
                StartsAtUtc = DateTime.UtcNow.Date.AddDays(-4).AddHours(9),
                MeetingPoint = "Clubhouse",
                Location = "Holme Pierrepont National Watersports Centre",
                DistanceMiles = "4",
                PaceGroups = "Mixed"
            };
            db.Activities.Add(pastRun);
            db.ActivityAttendances.Add(new ActivityAttendance { ActivityId = pastRun.Id, UserId = admin.Id, Status = AttendanceStatus.Going });
            db.ActivityAttendances.Add(new ActivityAttendance { ActivityId = pastRun.Id, UserId = member.Id, Status = AttendanceStatus.Going });
            if (super is not null)
                db.ActivityAttendances.Add(new ActivityAttendance { ActivityId = pastRun.Id, UserId = super.Id, Status = AttendanceStatus.Going });

            await EnsurePastTestActivitiesAsync(db, club.Id, admin.Id);

            await db.SaveChangesAsync();
        }
        else
        {
            var hprc = await db.Clubs.FirstOrDefaultAsync(c => c.Name == "City Striders" || c.Name == "Holme Pierrepont Running Club");
            if (hprc is not null)
            {
                hprc.Name = "Holme Pierrepont Running Club";
                hprc.Description = "Holme Pierrepont Running Club";
                hprc.Location = "Holme Pierrepont, Nottingham";
                hprc.LogoUrl = "/clubs/hprc-logo.png";
                await db.SaveChangesAsync();
            }

            var clubRun = await db.Activities.FirstOrDefaultAsync(r =>
                r.Title == "Wednesday Club Run" && r.IsActive && r.Kind == ActivityKind.ClubActivity);
            if (clubRun is not null)
            {
                if (string.IsNullOrEmpty(clubRun.Location))
                {
                    clubRun.Location = "Holme Pierrepont National Watersports Centre";
                }

                if (!await db.VolunteerSlots.AnyAsync(s => s.ActivityId == clubRun.Id))
                {
                    db.VolunteerSlots.AddRange(
                        new VolunteerSlot { ActivityId = clubRun.Id, Role = "Timekeeper", Description = "Record finish times at the clubhouse" },
                        new VolunteerSlot { ActivityId = clubRun.Id, Role = "Tail runner", Description = "Activity at the back of the group" },
                        new VolunteerSlot { ActivityId = clubRun.Id, Role = "Setup", Description = "Help set up the start area" });
                }
            }

            var hillsRun = await db.Activities.FirstOrDefaultAsync(r =>
                r.Title == "Tuesday Hills" && r.IsActive && r.Kind == ActivityKind.ClubActivity);
            if (hillsRun is not null && string.IsNullOrEmpty(hillsRun.Location))
            {
                hillsRun.Location = "Holme Pierrepont, Nottingham";
            }

            if (hprc is not null
                && !await db.Activities.AnyAsync(r => r.Title == "Black Rocks Fell Race" && r.ClubId == hprc.Id))
            {
                var adminUserId = await db.ClubMemberships
                    .Where(m => m.ClubId == hprc.Id && m.Role == ClubRole.Admin && m.IsActive)
                    .Select(m => m.UserId)
                    .FirstOrDefaultAsync();

                if (adminUserId is not null)
                {
                    db.Activities.Add(new Activity
                    {
                        Kind = ActivityKind.Race,
                        ClubId = hprc.Id,
                        CreatedByUserId = adminUserId,
                        Title = "Black Rocks Fell Race",
                        Description = "Challenging fell race over rocky terrain with Peak District views",
                        StartsAtUtc = new DateTime(2026, 9, 2, 18, 0, 0, DateTimeKind.Utc),
                        MeetingPoint = "Black Rocks car park",
                        Location = "Black Rocks, Peak District",
                        DistanceMiles = "5.5",
                        RunType = "Fell race",
                        VolunteerSlots =
                        [
                            new VolunteerSlot { Role = "Marshal", Tag = "marshall point 4", Description = "Station at a key point on the course" },
                            new VolunteerSlot { Role = "Registration", Description = "Sign runners in before the start" }
                        ]
                    });
                }
            }

            if (hprc is not null)
            {
                var sundaySocials = await db.Activities
                    .Where(r => r.ClubId == hprc.Id && r.IsActive && r.Title.StartsWith("Sunday social"))
                    .OrderBy(r => r.CreatedAtUtc)
                    .ToListAsync();
                var keepSunday = sundaySocials.FirstOrDefault(r => r.Title == "Sunday social run")
                    ?? sundaySocials.FirstOrDefault();
                foreach (var extra in sundaySocials.Where(r => keepSunday is null || r.Id != keepSunday.Id))
                    extra.IsActive = false;

                if (keepSunday is null)
                {
                    var createdBy = await db.ClubMemberships
                        .Where(m => m.ClubId == hprc.Id && m.Role == ClubRole.Admin && m.IsActive)
                        .Select(m => m.UserId)
                        .FirstOrDefaultAsync()
                        ?? await db.ClubMemberships
                            .Where(m => m.ClubId == hprc.Id && m.IsActive)
                            .Select(m => m.UserId)
                            .FirstOrDefaultAsync();

                    if (createdBy is not null)
                    {
                        var pastRun = new Activity
                        {
                            Kind = ActivityKind.ClubActivity,
                            ClubId = hprc.Id,
                            CreatedByUserId = createdBy,
                            Title = "Sunday social run",
                            Description = "Easy recovery run around the lake",
                            StartsAtUtc = DateTime.UtcNow.Date.AddDays(-4).AddHours(9),
                            MeetingPoint = "Clubhouse",
                            Location = "Holme Pierrepont National Watersports Centre",
                            DistanceMiles = "4",
                            PaceGroups = "Mixed"
                        };
                        db.Activities.Add(pastRun);

                        var rsvpUserIds = await db.ClubMemberships
                            .Where(m => m.ClubId == hprc.Id && m.IsActive)
                            .Select(m => m.UserId)
                            .Distinct()
                            .ToListAsync();
                        foreach (var userId in rsvpUserIds)
                        {
                            db.ActivityAttendances.Add(new ActivityAttendance
                            {
                                ActivityId = pastRun.Id,
                                UserId = userId,
                                Status = AttendanceStatus.Going
                            });
                        }
                    }
                }
            }

            if (hprc is not null && super is not null
                && !await db.ClubMemberships.AnyAsync(m => m.UserId == super.Id && m.ClubId == hprc.Id))
            {
                db.ClubMemberships.Add(new ClubMembership
                {
                    UserId = super.Id,
                    ClubId = hprc.Id,
                    Role = ClubRole.SuperAdmin
                });
            }

            if (hprc is not null && super is not null)
            {
                var superMembership = await db.ClubMemberships.FirstOrDefaultAsync(m =>
                    m.UserId == super.Id && m.ClubId == hprc.Id);
                if (superMembership is not null)
                    superMembership.Role = ClubRole.SuperAdmin;
            }

            if (hprc is not null && !await db.ValidateMembers.AnyAsync())
            {
                db.ValidateMembers.Add(new ValidateMember
                {
                    ClubId = hprc.Id,
                    FirstName = "Jane",
                    LastName = "Doe",
                    EnglandAthleticsNumber = "1234567",
                    Role = ClubRole.Member
                });
            }

            if (hprc is not null)
            {
                var createdBy = await db.ClubMemberships
                    .Where(m => m.ClubId == hprc.Id && m.Role == ClubRole.Admin && m.IsActive)
                    .Select(m => m.UserId)
                    .FirstOrDefaultAsync()
                    ?? await db.ClubMemberships
                        .Where(m => m.ClubId == hprc.Id && m.IsActive)
                        .Select(m => m.UserId)
                        .FirstOrDefaultAsync();
                if (createdBy is not null)
                    await EnsurePastTestActivitiesAsync(db, hprc.Id, createdBy);

                await EnsureDefaultVolunteerRoleTypesAsync(db, hprc.Id);
            }

            await db.SaveChangesAsync();
        }
    }

    private static void AddDefaultVolunteerRoleTypes(AppDbContext db, Guid clubId)
    {
        foreach (var (name, description) in VolunteerRoleCatalog.Defaults)
        {
            db.VolunteerRoleTypes.Add(new VolunteerRoleType
            {
                ClubId = clubId,
                Name = name,
                Description = description
            });
        }
    }

    private static async Task EnsureDefaultVolunteerRoleTypesAsync(AppDbContext db, Guid clubId)
    {
        if (await db.VolunteerRoleTypes.AnyAsync(t => t.ClubId == clubId))
            return;
        AddDefaultVolunteerRoleTypes(db, clubId);
    }

    private static async Task EnsurePastTestActivitiesAsync(AppDbContext db, Guid clubId, string createdByUserId)
    {
        var samples = new (string Title, string Description, int DaysAgo, string Miles, string MeetingPoint, bool Training)[]
        {
            ("Thursday tempo", "Steady tempo around the lake", 7, "6", "Clubhouse", true),
            ("Saturday long run", "Easy long run along the canal", 10, "10", "Trent Basin", false),
            ("Track Tuesday", "Intervals at Harvey Hadden", 14, "5", "Harvey Hadden stadium", true),
            ("Monday recovery jog", "Very easy shakeout with coffee after", 18, "3", "Clubhouse", false)
        };

        var memberIds = await db.ClubMemberships
            .Where(m => m.ClubId == clubId && m.IsActive)
            .Select(m => m.UserId)
            .Distinct()
            .ToListAsync();

        foreach (var sample in samples)
        {
            if (await db.Activities.AnyAsync(a => a.ClubId == clubId && a.Title == sample.Title && a.IsActive))
                continue;

            var activity = new Activity
            {
                Kind = ActivityKind.ClubActivity,
                ClubId = clubId,
                CreatedByUserId = createdByUserId,
                Title = sample.Title,
                Description = sample.Description,
                StartsAtUtc = DateTime.UtcNow.Date.AddDays(-sample.DaysAgo).AddHours(9),
                MeetingPoint = sample.MeetingPoint,
                Location = "Holme Pierrepont, Nottingham",
                DistanceMiles = sample.Miles,
                PaceGroups = "Mixed",
                IsTrainingSession = sample.Training,
                SessionType = sample.Training ? TrainingSessionType.Tempo : null
            };
            db.Activities.Add(activity);
            foreach (var userId in memberIds)
            {
                db.ActivityAttendances.Add(new ActivityAttendance
                {
                    ActivityId = activity.Id,
                    UserId = userId,
                    Status = AttendanceStatus.Going
                });
            }
        }
    }
}
