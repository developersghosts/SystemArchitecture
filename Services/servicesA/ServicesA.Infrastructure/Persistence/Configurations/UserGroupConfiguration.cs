using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicesA.Domain.Entities.Identity;

namespace ServicesA.Infrastructure.Persistence.Configurations;

public class UserGroupConfiguration : IEntityTypeConfiguration<UserGroup>
{
    public void Configure(EntityTypeBuilder<UserGroup> builder)
    {
        builder.ToTable("UserGroups");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();
        builder.Property(g => g.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(g => g.Name).IsUnique();
        builder.Property(g => g.Description).HasMaxLength(500);
        builder.Property(g => g.CreatedAtUtc).IsRequired();

        // The roles are part of the aggregate (kept through the private _roles list).
        builder.HasMany(g => g.Roles)
            .WithOne()
            .HasForeignKey(r => r.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(g => g.Roles).HasField("_roles").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(g => g.DomainEvents);
    }
}

public class UserGroupRoleConfiguration : IEntityTypeConfiguration<UserGroupRole>
{
    public void Configure(EntityTypeBuilder<UserGroupRole> builder)
    {
        builder.ToTable("UserGroupRoles");
        builder.HasKey(r => new { r.GroupId, r.RoleId });
        builder.HasIndex(r => r.RoleId);

        builder.HasOne<ApplicationRole>()
            .WithMany()
            .HasForeignKey(r => r.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserGroupMemberConfiguration : IEntityTypeConfiguration<UserGroupMember>
{
    public void Configure(EntityTypeBuilder<UserGroupMember> builder)
    {
        builder.ToTable("UserGroupMembers");
        builder.HasKey(m => new { m.GroupId, m.UserId });
        builder.HasIndex(m => m.UserId);

        builder.HasOne<UserGroup>()
            .WithMany()
            .HasForeignKey(m => m.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
