using System.Text;
using BuildingBlocks.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ServicesA.Application.Abstractions;
using ServicesA.Domain.Entities.Identity;
using ServicesA.Infrastructure.Email;
using ServicesA.Infrastructure.Identity;
using ServicesA.Infrastructure.Persistence;
using ServicesA.Infrastructure.Security;
using ServicesA.Infrastructure.Localization;
using Microsoft.AspNetCore.DataProtection;

namespace ServicesA.Infrastructure;

public static class ServiceInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // EF Core
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // ASP.NET Core Identity (no cookies; JWT only)
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;

                options.User.RequireUniqueEmail = true;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders(); // password reset tokens

        // Data Protection: encrypts secrets saved in the database and Identity tokens (reset links).
        // Keys must survive restarts, otherwise saved secrets and emailed links stop working.
        var dataProtection = services.AddDataProtection().SetApplicationName("ServicesA");
        var keysPath = configuration["DataProtection:KeysPath"];
        if (!string.IsNullOrWhiteSpace(keysPath))
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();

        // Message dictionary (Egyptian / Saudi / English) from the database, kept in memory
        services.AddSingleton<DbLocalizedTextStore>();
        services.AddSingleton<BuildingBlocks.Localization.ILocalizedTextStore>(sp => sp.GetRequiredService<DbLocalizedTextStore>());
        services.AddSingleton<ILocalizedTextCache>(sp => sp.GetRequiredService<DbLocalizedTextStore>());

        // Email: settings from the database (Settings > Email), configuration as fallback; cached in memory
        services.AddMemoryCache();
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.AddScoped<IEmailSettingsProvider, EmailSettingsProvider>();
        services.AddScoped<IEmailService, SmtpEmailService>();

        // JWT
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Key) && Encoding.UTF8.GetByteCount(o.Key) >= 32,
                "JWT:Key must be at least 32 bytes.")
            .Validate(o => o.Lifetime > TimeSpan.Zero, "Set JWT:DurationInMinutes (or JWT:DurationInDays) to more than 0.")
            .ValidateOnStart();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddJwtAuthentication(configuration);

        return services;
    }

    private static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false; // keep "sub", "role", "sid" as written
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key ?? string.Empty)),
                    NameClaimType = JwtClaims.Name,
                    RoleClaimType = JwtClaims.Role,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                // A signed token is not enough: it must belong to the user's current session and security stamp.
                // This makes logout, forced login, password change/reset and deactivation take effect immediately.
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var principal = context.Principal!;
                        if (!Guid.TryParse(principal.FindFirst(JwtClaims.UserId)?.Value, out var userId) ||
                            !Guid.TryParse(principal.FindFirst(JwtClaims.SessionId)?.Value, out var sessionId))
                        {
                            context.Fail("Invalid token claims.");
                            return;
                        }

                        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                        var user = await userManager.FindByIdAsync(userId.ToString());
                        var stamp = principal.FindFirst(JwtClaims.SecurityStamp)?.Value;

                        if (user is null || !user.IsSessionValid(sessionId, DateTime.UtcNow) || user.SecurityStamp != stamp)
                            context.Fail("The session has ended.");
                    }
                };
            });
    }
}
