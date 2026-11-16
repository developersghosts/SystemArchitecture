using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ServicesA.Domain.Entities.Identity;
using ServicesA.Domain.Entities.Settings;

namespace ServicesA.Infrastructure.Persistence;

/// <summary>Bound from "Seed:Admin". The admin is only created when Password is set (keep it in .env / user secrets).</summary>
public sealed class AdminSeedOptions
{
    public const string SectionName = "Seed:Admin";

    public string UserName { get; set; } = "admin";
    public string Email { get; set; } = "admin@localhost";
    public string FirstName { get; set; } = "System";
    public string LastName { get; set; } = "Administrator";
    public DateOnly? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Password { get; set; }
}

/// <summary>
/// Runs at startup when "Database:InitializeOnStartup" is true:
/// applies pending migrations, creates the Identity roles and the first Administrator.
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>Adds the dictionary texts that are missing; never changes texts that already exist (edited by users).</summary>
    private static async Task SeedMessageDictionaryAsync(AppDbContext db, ILogger logger, CancellationToken ct)
    {
        var existing = (await db.LocalizedTexts.Select(t => new { t.Key, t.Culture }).ToListAsync(ct))
            .Select(t => $"{t.Culture}|{t.Key}").ToHashSet();

        var added = 0;
        foreach (var e in Seed.LocalizedTextSeed.Entries)
        {
            foreach (var (culture, value) in new[]
                     { (LocalizedText.English, e.En), (LocalizedText.Egyptian, e.Egyptian), (LocalizedText.Saudi, e.Saudi) })
            {
                if (existing.Contains($"{culture}|{e.Key}")) continue;
                db.LocalizedTexts.Add(LocalizedText.Create(e.Key, culture, value));
                added++;
            }
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Message dictionary: {Count} texts added", added);
        }
    }

    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));

        var db = provider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Database migrations applied");

        var roleManager = provider.GetRequiredService<RoleManager<ApplicationRole>>();
        foreach (var (name, title) in IdentityRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(name))
                await roleManager.CreateAsync(ApplicationRole.Create(name, title));
        }

        await SeedMessageDictionaryAsync(db, logger, cancellationToken);

        var seed = provider.GetRequiredService<IConfiguration>().GetSection(AdminSeedOptions.SectionName).Get<AdminSeedOptions>()
                   ?? new AdminSeedOptions();
        if (string.IsNullOrWhiteSpace(seed.Password))
        {
            logger.LogInformation("Seed:Admin:Password is not set; no administrator was created");
            return;
        }

        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByNameAsync(seed.UserName) is not null)
            return;

        var admin = ApplicationUser.Create(seed.UserName, seed.Email, seed.FirstName, seed.LastName,
            seed.DateOfBirth, seed.PhoneNumber);
        var created = await userManager.CreateAsync(admin, seed.Password);
        if (!created.Succeeded)
        {
            logger.LogError("Creating the administrator failed: {Errors}",
                string.Join(" ", created.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(admin, IdentityRoles.Administrator);
        logger.LogInformation("Administrator {UserName} created", seed.UserName);
    }
}
