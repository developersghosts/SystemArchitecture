using ServicesA.Domain.Common;

namespace ServicesA.Domain.Auditing;

/// <summary>History of login attempts for a user.</summary>
public sealed class LoginHistory : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public DateTime LoggedInAtUtc { get; private set; }
    public bool Succeeded { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    private LoginHistory() { }

    public static LoginHistory Create(Guid userId, bool succeeded, string? ipAddress, string? userAgent) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = userId,
        Succeeded = succeeded,
        IpAddress = ipAddress,
        UserAgent = userAgent,
        LoggedInAtUtc = DateTime.UtcNow
    };
}
