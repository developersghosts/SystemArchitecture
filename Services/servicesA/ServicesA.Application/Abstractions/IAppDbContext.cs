using ServicesA.Domain.Auditing;
using ServicesA.Domain.Entities.Identity;
using ServicesA.Domain.Entities.Settings;

namespace ServicesA.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<ApplicationUser> Users { get; }
    DbSet<ApplicationRole> Roles { get; }
    DbSet<ApplicationUserRole> UserRoles { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<LoginHistory> LoginHistories { get; }
    DbSet<ProjectConfiguration> ProjectConfigurations { get; }
    DbSet<EmailServerSettings> EmailServerSettings { get; }
    DbSet<LocalizedText> LocalizedTexts { get; }
    DbSet<UserGroup> UserGroups { get; }
    DbSet<UserGroupRole> UserGroupRoles { get; }
    DbSet<UserGroupMember> UserGroupMembers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
