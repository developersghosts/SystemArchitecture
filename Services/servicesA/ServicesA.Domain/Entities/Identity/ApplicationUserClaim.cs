using Microsoft.AspNetCore.Identity;

namespace ServicesA.Domain.Entities.Identity;

/// <summary>Maps to the AspNetUserClaims table.</summary>
public class ApplicationUserClaim : IdentityUserClaim<Guid>
{
}
