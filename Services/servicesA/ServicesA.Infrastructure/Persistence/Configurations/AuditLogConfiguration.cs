using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicesA.Domain.Auditing;

namespace ServicesA.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.EntityName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.EntityId).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Action).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(a => new { a.EntityName, a.EntityId });
        builder.HasIndex(a => a.OccurredAtUtc);
    }
}
