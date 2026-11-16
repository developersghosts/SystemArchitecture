using ServicesA.Application.Features.Identity.Common;

namespace ServicesA.Application.Features.Identity.UserRoles.Commands.CreateUserRole;

/// <summary>Adds roles to a user. Returns the added role ids. New roles appear in the JWT at the next login.</summary>
public sealed record CreateUserRoleCommand(Guid UserId, List<Guid> RoleIds) : ICommand<List<Guid>>;

public sealed class CreateUserRoleValidator : AbstractValidator<CreateUserRoleCommand>
{
    public CreateUserRoleValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RoleIds).NotEmpty();
        RuleForEach(x => x.RoleIds).NotEmpty();
    }
}

public sealed class CreateUserRoleHandler(
    UserManager<ApplicationUser> userManager,
    IAppDbContext db,
    UserRegistrar registrar,
    IStringLocalizerService localizer) : ICommandHandler<CreateUserRoleCommand, List<Guid>>
{
    public async Task<List<Guid>> Handle(CreateUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString())
                   ?? throw new NotFoundException(localizer.Get(ResourceKeys.UserNotFound));

        var roleIds = request.RoleIds.Distinct().ToList();
        var roleNames = await registrar.ResolveRoleNamesAsync(roleIds, cancellationToken);

        var alreadyAssigned = await db.UserRoles
            .Where(ur => ur.UserId == user.Id && roleIds.Contains(ur.RoleId))
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name!)
            .ToListAsync(cancellationToken);

        if (alreadyAssigned.Count > 0)
            throw new BadRequestException($"{localizer.Get(ResourceKeys.RolesAlreadyAssigned)} {string.Join(", ", alreadyAssigned)}");

        (await userManager.AddToRolesAsync(user, roleNames)).EnsureSucceeded();
        return roleIds;
    }
}
