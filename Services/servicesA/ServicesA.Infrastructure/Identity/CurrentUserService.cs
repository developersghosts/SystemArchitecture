using Microsoft.AspNetCore.Http;
using ServicesA.Application.Abstractions;

namespace ServicesA.Infrastructure.Identity;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private HttpContext? Context => httpContextAccessor.HttpContext;

    public Guid? UserId =>
        Guid.TryParse(Context?.User.FindFirst(JwtClaims.UserId)?.Value, out var id) ? id : null;

    public Guid? SessionId =>
        Guid.TryParse(Context?.User.FindFirst(JwtClaims.SessionId)?.Value, out var id) ? id : null;

    public string? IpAddress => Context?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent
    {
        get
        {
            var value = Context?.Request.Headers.UserAgent.ToString();
            return string.IsNullOrEmpty(value) ? null : value[..Math.Min(value.Length, 512)];
        }
    }
}
