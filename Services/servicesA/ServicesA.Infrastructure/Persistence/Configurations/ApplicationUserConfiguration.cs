using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicesA.Domain.Entities.Identity;

namespace ServicesA.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ComplexProperty(u => u.FullName, name =>
        {
            name.Property(n => n.FirstName).HasColumnName("FirstName").HasMaxLength(100).IsRequired();
            name.Property(n => n.LastName).HasColumnName("LastName").HasMaxLength(100).IsRequired();
        });

        builder.Property(u => u.IsActive).HasDefaultValue(true);
        builder.Property(u => u.CreatedAtUtc).IsRequired();

        builder.Property(u => u.OtpCode).HasMaxLength(10);
        builder.Property(u => u.OtpFailedAttempts).HasDefaultValue(0);

        builder.Ignore(u => u.DomainEvents);
    }
}
