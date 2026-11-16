using ServicesA.Domain.Common;

namespace ServicesA.Domain.Entities.Identity.Events;

public sealed record UserRegisteredDomainEvent(Guid UserId, string Email) : DomainEvent;
