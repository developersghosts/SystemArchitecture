using ServicesA.Domain.Entities.Identity;

namespace ServicesA.Application.Abstractions;

public interface IJwtTokenService
{
    /// <summary>Token lifetime from the JWT options.</summary>
    TimeSpan Lifetime { get; }

    string CreateToken(ApplicationUser user, IEnumerable<string> roles, Guid sessionId, DateTime expiresAtUtc);
}
