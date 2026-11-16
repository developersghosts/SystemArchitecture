namespace ServicesA.Application.Features.Identity.Account.Commands.Logout;

/// <summary>Ends the current session; the JWT stops being accepted immediately.</summary>
public sealed record LogoutCommand : ICommand<bool>;

public sealed class LogoutHandler(ICurrentUserService currentUser, IAppDbContext db)
    : ICommandHandler<LogoutCommand, bool>
{
    public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
            return true;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return true;

        user.EndSession();
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
