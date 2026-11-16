namespace ServicesA.Domain.Common;

/// <summary>Implemented by aggregate roots that raise domain events.</summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
