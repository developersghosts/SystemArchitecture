namespace ServicesA.Domain.Common;

public interface IAuditableEntity
{
    DateTime CreatedAtUtc { get; }
    DateTime? ModifiedAtUtc { get; }

    void SetCreated(DateTime utcNow);
    void SetModified(DateTime utcNow);
}
