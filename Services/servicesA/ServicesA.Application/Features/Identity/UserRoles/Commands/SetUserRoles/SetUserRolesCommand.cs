using ServicesA.Application.Features.Identity.Common;

namespace ServicesA.Application.Features.Identity.UserRoles.Commands.SetUserRoles;

/// <summary>
/// Replaces the user's roles with exactly <see cref="RoleIds"/> (adds the missing ones, removes the rest).
/// When roles are removed the security stamp changes, so the user's current token stops working at once.
/// </summary>
public sealed record SetUserRolesCommand(Guid UserId, List<Guid> RoleIds) : ICommand<List<UserRoleDto>>;

public sealed class SetUserRolesValidator : AbstractValidator<SetUserRolesCommand>
{
    public SetUserRolesValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RoleIds).NotNull();
        RuleForEach(x => x.RoleIds).NotEmpty();
    }
}

public sealed class SetUserRolesHandler(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    UserRegistrar registrar,
    UserRoleResolver roleResolver,
    ICurrentUserService currentUser,
    IStringLocalizerService localizer) : ICommandHandler<SetUserRolesCommand, List<UserRoleDto>>
{
    public async Task<List<UserRoleDto>> Handle(SetUserRolesCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString())
                   ?? throw new NotFoundException(localizer.Get(ResourceKeys.UserNotFound));

        var wanted = await registrar.ResolveRoleNamesAsync(request.RoleIds, cancellationToken);
        var current = await userManager.GetRolesAsync(user);

        // You cannot lock yourself out: keep Administrator directly or through one of your groups.
        var isSelf = currentUser.UserId == user.Id;
        if (isSelf && current.Contains(IdentityRoles.Administrator) && !wanted.Contains(IdentityRoles.Administrator) &&
            !(await roleResolver.GroupRoleNamesAsync(user.Id, null, cancellationToken)).Contains(IdentityRoles.Administrator))
            throw new BadRequestException(localizer.Get(ResourceKeys.CannotRemoveOwnAdministrator), "CANNOT_REMOVE_OWN_ADMIN");

        var toAdd = wanted.Except(current).ToList();
        var toRemove = current.Except(wanted).ToList();

        if (toAdd.Count > 0)
            (await userManager.AddToRolesAsync(user, toAdd)).EnsureSucceeded();

        if (toRemove.Count > 0)
        {
            (await userManager.RemoveFromRolesAsync(user, toRemove)).EnsureSucceeded();
            // The old token still lists the removed roles: make it invalid (the user signs in again).
            if (!isSelf)
                await userManager.UpdateSecurityStampAsync(user);
        }

        var fullName = user.FullName.ToString();
        return await roleManager.Roles.AsNoTracking()
            .Where(r => wanted.Contains(r.Name!))
            .OrderBy(r => r.Name)
            .Select(r => new UserRoleDto(user.Id, user.UserName!, fullName, r.Id, r.Name!, r.Title))
            .ToListAsync(cancellationToken);
    }
}
