namespace ServicesA.Domain.Common;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOnUtc { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.CreateVersion7();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
