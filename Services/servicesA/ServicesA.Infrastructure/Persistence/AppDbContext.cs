using System.Text.Json;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ServicesA.Application.Abstractions;
using ServicesA.Domain.Auditing;
using ServicesA.Domain.Common;
using ServicesA.Domain.Entities.Identity;
using ServicesA.Domain.Entities.Settings;

namespace ServicesA.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid, ApplicationUserClaim, ApplicationUserRole,
        ApplicationUserLogin, ApplicationRoleClaim, ApplicationUserToken>(options), IAppDbContext
{
    private static readonly HashSet<string> SensitiveProperties =
        ["PasswordHash", "SecurityStamp", "ConcurrencyStamp", "OtpCode", "ActiveSessionId", "ProtectedPassword"];

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<LoginHistory> LoginHistories => Set<LoginHistory>();
    public DbSet<ProjectConfiguration> ProjectConfigurations => Set<ProjectConfiguration>();
    public DbSet<EmailServerSettings> EmailServerSettings => Set<EmailServerSettings>();
    public DbSet<LocalizedText> LocalizedTexts => Set<LocalizedText>();
    public DbSet<UserGroup> UserGroups => Set<UserGroup>();
    public DbSet<UserGroupRole> UserGroupRoles => Set<UserGroupRole>();
    public DbSet<UserGroupMember> UserGroupMembers => Set<UserGroupMember>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // creates the AspNet* Identity tables
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        ApplyAuditableTimestamps(now);
        AddAuditLogs(now);
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditableTimestamps(DateTime now)
    {
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added) entry.Entity.SetCreated(now);
            else if (entry.State == EntityState.Modified) entry.Entity.SetModified(now);
        }
    }

    private void AddAuditLogs(DateTime now)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is not AuditLog && e.Entity is not LoginHistory
                        && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Created,
                EntityState.Deleted => AuditAction.Deleted,
                _ => AuditAction.Updated
            };

            var oldValues = new Dictionary<string, object?>();
            var newValues = new Dictionary<string, object?>();

            foreach (var property in entry.Properties.Where(p => !SensitiveProperties.Contains(p.Metadata.Name)))
            {
                var name = property.Metadata.Name;
                if (entry.State != EntityState.Added) oldValues[name] = property.OriginalValue;
                if (entry.State != EntityState.Deleted) newValues[name] = property.CurrentValue;
            }

            // Value objects mapped as complex properties (e.g. Company.NameAr, Currency.Symbol, FullName.FirstName)
            foreach (var complex in entry.ComplexProperties)
            {
                foreach (var property in complex.Properties.Where(p => !SensitiveProperties.Contains(p.Metadata.Name)))
                {
                    var name = $"{complex.Metadata.Name}.{property.Metadata.Name}";
                    if (entry.State != EntityState.Added) oldValues[name] = Shorten(property.OriginalValue);
                    if (entry.State != EntityState.Deleted) newValues[name] = Shorten(property.CurrentValue);
                }
            }

            AuditLogs.Add(AuditLog.Create(
                entry.Metadata.ClrType.Name,
                GetPrimaryKey(entry),
                action,
                oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues) : null,
                newValues.Count > 0 ? JsonSerializer.Serialize(newValues) : null,
                now));
        }
    }

    /// <summary>Big values (a logo saved as a data URL) are not copied into the audit log.</summary>
    private static object? Shorten(object? value) =>
        value is string { Length: > 500 } text ? $"({text.Length} characters)" : value;

    private static string GetPrimaryKey(EntityEntry entry) =>
        string.Join(",", entry.Metadata.FindPrimaryKey()!.Properties.Select(p => entry.Property(p.Name).CurrentValue));
}
