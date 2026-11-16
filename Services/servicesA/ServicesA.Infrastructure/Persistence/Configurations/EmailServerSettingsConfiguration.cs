using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicesA.Domain.Entities.Settings;

namespace ServicesA.Infrastructure.Persistence.Configurations;

public class EmailServerSettingsConfiguration : IEntityTypeConfiguration<EmailServerSettings>
{
    public void Configure(EntityTypeBuilder<EmailServerSettings> builder)
    {
        builder.ToTable("EmailServerSettings");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.SmtpServer).HasMaxLength(255);
        builder.Property(s => s.Security).HasMaxLength(20).IsRequired();
        builder.Property(s => s.Username).HasMaxLength(256);
        builder.Property(s => s.ProtectedPassword).HasMaxLength(2000); // Data Protection payload
        builder.Property(s => s.SenderEmail).HasMaxLength(256);
        builder.Property(s => s.ResetPasswordUrl).HasMaxLength(500);
        builder.Property(s => s.CreatedAtUtc).IsRequired();
        builder.Ignore(s => s.HasPassword);
        builder.Ignore(s => s.DomainEvents);
    }
}
