using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicesA.Domain.Entities.Identity;

namespace ServicesA.Infrastructure.Persistence.Configurations;

public class ApplicationUserRoleConfiguration : IEntityTypeConfiguration<ApplicationUserRole>
{
    public void Configure(EntityTypeBuilder<ApplicationUserRole> builder) => builder.ToTable("AspNetUserRoles");
}

public class ApplicationUserClaimConfiguration : IEntityTypeConfiguration<ApplicationUserClaim>
{
    public void Configure(EntityTypeBuilder<ApplicationUserClaim> builder) => builder.ToTable("AspNetUserClaims");
}

public class ApplicationUserLoginConfiguration : IEntityTypeConfiguration<ApplicationUserLogin>
{
    public void Configure(EntityTypeBuilder<ApplicationUserLogin> builder) => builder.ToTable("AspNetUserLogins");
}

public class ApplicationUserTokenConfiguration : IEntityTypeConfiguration<ApplicationUserToken>
{
    public void Configure(EntityTypeBuilder<ApplicationUserToken> builder) => builder.ToTable("AspNetUserTokens");
}

public class ApplicationRoleClaimConfiguration : IEntityTypeConfiguration<ApplicationRoleClaim>
{
    public void Configure(EntityTypeBuilder<ApplicationRoleClaim> builder) => builder.ToTable("AspNetRoleClaims");
}
