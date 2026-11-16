using Microsoft.AspNetCore.Identity;

namespace ServicesA.Domain.Entities.Identity;

/// <summary>Maps to the AspNetRoleClaims table.</summary>
public class ApplicationRoleClaim : IdentityRoleClaim<Guid>
{
}
