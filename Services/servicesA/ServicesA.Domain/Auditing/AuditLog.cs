using ServicesA.Domain.Common;

namespace ServicesA.Domain.Auditing;

/// <summary>History of data changes: which entity changed and what the values were.</summary>
public sealed class AuditLog : Entity<Guid>
{
    public string EntityName { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public AuditAction Action { get; private set; }
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(string entityName, string entityId, AuditAction action,
        string? oldValues, string? newValues, DateTime occurredAtUtc) => new()
    {
        Id = Guid.CreateVersion7(),
        EntityName = entityName,
        EntityId = entityId,
        Action = action,
        OldValues = oldValues,
        NewValues = newValues,
        OccurredAtUtc = occurredAtUtc
    };
}
