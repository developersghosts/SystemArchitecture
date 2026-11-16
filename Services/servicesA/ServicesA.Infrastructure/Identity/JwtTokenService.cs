using System.Security.Claims;
using System.Text;
using BuildingBlocks.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ServicesA.Application.Abstractions;
using ServicesA.Domain.Entities.Identity;

namespace ServicesA.Infrastructure.Identity;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;

    public TimeSpan Lifetime => _options.Lifetime;

    public string CreateToken(ApplicationUser user, IEnumerable<string> roles, Guid sessionId, DateTime expiresAtUtc)
    {
        var claims = new List<Claim>
        {
            new(JwtClaims.UserId, user.Id.ToString()),
            new(JwtClaims.Name, user.UserName!),
            new(JwtClaims.Email, user.Email!),
            new(JwtClaims.SessionId, sessionId.ToString()),
            new(JwtClaims.SecurityStamp, user.SecurityStamp ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(JwtClaims.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key!));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = DateTime.UtcNow,
            Expires = expiresAtUtc,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}

/// <summary>Claim names written into and read from the JWT (inbound claim mapping is turned off).</summary>
public static class JwtClaims
{
    public const string UserId = JwtRegisteredClaimNames.Sub;
    public const string Name = "name";
    public const string Email = JwtRegisteredClaimNames.Email;
    public const string Role = "role";
    public const string SessionId = JwtRegisteredClaimNames.Sid;
    public const string SecurityStamp = "security_stamp";
}
