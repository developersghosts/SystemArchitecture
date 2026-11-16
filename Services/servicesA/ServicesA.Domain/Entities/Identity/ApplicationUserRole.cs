using Microsoft.AspNetCore.Identity;

namespace ServicesA.Domain.Entities.Identity;

/// <summary>Maps to the AspNetUserRoles table.</summary>
public class ApplicationUserRole : IdentityUserRole<Guid>
{
}
