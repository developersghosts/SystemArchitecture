namespace BuildingBlocks.Messaging.Events;

/// <summary>Base type for events published between services through the message broker.</summary>
public record IntegrationEvent
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
    public string EventType => GetType().AssemblyQualifiedName!;
}
