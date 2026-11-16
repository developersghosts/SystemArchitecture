using ServicesA.Application.Features.Identity.Common;

namespace ServicesA.Application.Features.Identity.Account.Commands.RegisterUserFull;

/// <summary>Creates a user together with its roles and groups in one call.</summary>
public sealed record RegisterUserFullCommand(
    string FirstName,
    string LastName,
    string UserName,
    string Email,
    string Password,
    DateOnly? DateOfBirth,
    string? PhoneNumber,
    List<Guid>? RoleIds,
    List<Guid>? GroupIds = null) : ICommand<Guid>;

public sealed class RegisterUserFullValidator : AbstractValidator<RegisterUserFullCommand>
{
    public RegisterUserFullValidator()
    {
        RuleFor(x => x.FirstName).ValidName();
        RuleFor(x => x.LastName).ValidName();
        RuleFor(x => x.UserName).ValidUserName();
        RuleFor(x => x.Email).ValidEmail();
        RuleFor(x => x.Password).ValidPassword();
        RuleFor(x => x.PhoneNumber).MaximumLength(30);
        RuleForEach(x => x.RoleIds).NotEmpty();
        RuleForEach(x => x.GroupIds).NotEmpty();
    }
}

public sealed class RegisterUserFullHandler(
    UserRegistrar registrar,
    IAppDbContext db,
    IStringLocalizerService localizer,
    TimeProvider clock) : ICommandHandler<RegisterUserFullCommand, Guid>
{
    public async Task<Guid> Handle(RegisterUserFullCommand request, CancellationToken cancellationToken)
    {
        // Check the groups before creating anything.
        var groupIds = (request.GroupIds ?? []).Distinct().ToList();
        if (groupIds.Count > 0 &&
            await db.UserGroups.CountAsync(g => groupIds.Contains(g.Id), cancellationToken) != groupIds.Count)
            throw new BadRequestException(localizer.Get(ResourceKeys.GroupNotFound));

        var user = await registrar.RegisterAsync(
            new NewUser(request.FirstName, request.LastName, request.UserName, request.Email, request.Password,
                request.DateOfBirth, request.PhoneNumber),
            request.RoleIds, cancellationToken);

        if (groupIds.Count > 0)
        {
            var now = clock.GetUtcNow().UtcDateTime;
            foreach (var groupId in groupIds)
                db.UserGroupMembers.Add(UserGroupMember.Create(groupId, user.Id, now));
            await db.SaveChangesAsync(cancellationToken);
        }

        return user.Id;
    }
}
