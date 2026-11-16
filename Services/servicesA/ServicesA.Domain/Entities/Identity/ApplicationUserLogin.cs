using Microsoft.AspNetCore.Identity;

namespace ServicesA.Domain.Entities.Identity;

/// <summary>Maps to the AspNetUserLogins table.</summary>
public class ApplicationUserLogin : IdentityUserLogin<Guid>
{
}
