namespace ServicesA.Application.Abstractions;

/// <summary>Information about the caller of the current HTTP request.</summary>
public interface ICurrentUserService
{
    /// <summary>The user id from the JWT, or null when the request is anonymous.</summary>
    Guid? UserId { get; }
    /// <summary>The session id ("sid") from the JWT, or null.</summary>
    Guid? SessionId { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
}
