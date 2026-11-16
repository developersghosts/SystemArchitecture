using ServicesA.Application.Features.Identity.Common;

namespace ServicesA.Application.Features.Identity.Account.Commands.RefreshSession;

/// <summary>
/// Renews the caller's session for another JWT lifetime (2 hours) and returns a new token for the same session.
/// Needs a token that is still valid; the roles are read again, so role/group changes are picked up.
/// </summary>
public sealed record RefreshSessionCommand : ICommand<AuthResponse>;

public sealed class RefreshSessionHandler(
    ICurrentUserService currentUser,
    UserManager<ApplicationUser> userManager,
    IAppDbContext db,
    IJwtTokenService jwt,
    UserRoleResolver roleResolver,
    IStringLocalizerService localizer,
    TimeProvider clock) : ICommandHandler<RefreshSessionCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RefreshSessionCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || currentUser.SessionId is not { } sessionId)
            throw new BadRequestException(localizer.Get(ResourceKeys.SessionCannotBeRenewed), "SESSION_NOT_RENEWABLE");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                   ?? throw new BadRequestException(localizer.Get(ResourceKeys.SessionCannotBeRenewed), "SESSION_NOT_RENEWABLE");

        var now = clock.GetUtcNow().UtcDateTime;
        var expiresAt = now.Add(jwt.Lifetime);
        if (!user.RenewSession(sessionId, expiresAt, now))
            throw new BadRequestException(localizer.Get(ResourceKeys.SessionCannotBeRenewed), "SESSION_NOT_RENEWABLE");

        await db.SaveChangesAsync(cancellationToken);

        var roles = await roleResolver.GetEffectiveRolesAsync(user, cancellationToken);
        var token = jwt.CreateToken(user, roles, sessionId, expiresAt);

        return new AuthResponse(user.Id, user.UserName!, user.FullName.ToString(), user.Email!,
            user.DateOfBirth, roles, token, expiresAt);
    }
}
