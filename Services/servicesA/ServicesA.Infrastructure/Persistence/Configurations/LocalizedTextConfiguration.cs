using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicesA.Domain.Entities.Settings;

namespace ServicesA.Infrastructure.Persistence.Configurations;

public class LocalizedTextConfiguration : IEntityTypeConfiguration<LocalizedText>
{
    public void Configure(EntityTypeBuilder<LocalizedText> builder)
    {
        builder.ToTable("LocalizedTexts");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.Key).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Culture).HasMaxLength(10).IsRequired();
        builder.Property(t => t.Value).HasMaxLength(LocalizedText.MaxLength).IsRequired();
        builder.Property(t => t.CreatedAtUtc).IsRequired();
        builder.HasIndex(t => new { t.Key, t.Culture }).IsUnique();
    }
}
