using Microsoft.AspNetCore.Identity;

namespace ServicesA.Domain.Entities.Identity;

/// <summary>Maps to the AspNetUserTokens table.</summary>
public class ApplicationUserToken : IdentityUserToken<Guid>
{
}
