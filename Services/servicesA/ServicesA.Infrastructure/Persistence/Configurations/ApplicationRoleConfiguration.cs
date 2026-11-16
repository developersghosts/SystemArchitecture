using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicesA.Domain.Entities.Identity;

namespace ServicesA.Infrastructure.Persistence.Configurations;

public class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.Property(r => r.Title).HasMaxLength(256).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(500);
    }
}
