using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicesA.Domain.Auditing;
using ServicesA.Domain.Entities.Identity;

namespace ServicesA.Infrastructure.Persistence.Configurations;

public class LoginHistoryConfiguration : IEntityTypeConfiguration<LoginHistory>
{
    public void Configure(EntityTypeBuilder<LoginHistory> builder)
    {
        builder.ToTable("LoginHistories");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();
        builder.Property(l => l.IpAddress).HasMaxLength(64);
        builder.Property(l => l.UserAgent).HasMaxLength(512);
        builder.HasIndex(l => new { l.UserId, l.LoggedInAtUtc });

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
