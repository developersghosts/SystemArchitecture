using ServicesA.Application.Features.Identity.Common;

namespace ServicesA.Application.Features.Identity.UserGroups.Commands;

public sealed class GroupBodyValidator<T> : AbstractValidator<T> where T : IGroupBody
{
    public GroupBodyValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.RoleIds).NotNull();
        RuleForEach(x => x.RoleIds).NotEmpty();
    }
}

public interface IGroupBody
{
    string Name { get; }
    string? Description { get; }
    List<Guid> RoleIds { get; }
}

// ---- Create ----------------------------------------------------------------------------------------------

public sealed record CreateUserGroupCommand(string Name, string? Description, List<Guid> RoleIds)
    : ICommand<UserGroupDetailsDto>, IGroupBody;

public sealed class CreateUserGroupValidator : AbstractValidator<CreateUserGroupCommand>
{
    public CreateUserGroupValidator() => Include(new GroupBodyValidator<CreateUserGroupCommand>());
}

public sealed class CreateUserGroupHandler(IAppDbContext db, UserRegistrar registrar, ISender sender,
    IStringLocalizerService localizer) : ICommandHandler<CreateUserGroupCommand, UserGroupDetailsDto>
{
    public async Task<UserGroupDetailsDto> Handle(CreateUserGroupCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await db.UserGroups.AnyAsync(g => g.Name == name, cancellationToken))
            throw new BadRequestException(localizer.Get(ResourceKeys.GroupNameExists), "GROUP_NAME_EXISTS");

        await registrar.ResolveRoleNamesAsync(request.RoleIds, cancellationToken); // every role must exist

        var group = UserGroup.Create(name, request.Description, request.RoleIds);
        db.UserGroups.Add(group);
        await db.SaveChangesAsync(cancellationToken);

        return await sender.Send(new Queries.GetUserGroupQuery(group.Id), cancellationToken);
    }
}

// ---- Update ----------------------------------------------------------------------------------------------

/// <summary>Renames the group and replaces its roles. When roles are removed, members sign in again.</summary>
public sealed record UpdateUserGroupCommand(Guid Id, string Name, string? Description, List<Guid> RoleIds)
    : ICommand<UserGroupDetailsDto>, IGroupBody;

public sealed class UpdateUserGroupValidator : AbstractValidator<UpdateUserGroupCommand>
{
    public UpdateUserGroupValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        Include(new GroupBodyValidator<UpdateUserGroupCommand>());
    }
}

public sealed class UpdateUserGroupHandler(
    IAppDbContext db,
    UserManager<ApplicationUser> userManager,
    UserRegistrar registrar,
    UserRoleResolver resolver,
    ICurrentUserService currentUser,
    ISender sender,
    IStringLocalizerService localizer) : ICommandHandler<UpdateUserGroupCommand, UserGroupDetailsDto>
{
    public async Task<UserGroupDetailsDto> Handle(UpdateUserGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await db.UserGroups.Include(g => g.Roles).FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken)
                    ?? throw new NotFoundException(localizer.Get(ResourceKeys.GroupNotFound));

        var name = request.Name.Trim();
        if (await db.UserGroups.AnyAsync(g => g.Name == name && g.Id != group.Id, cancellationToken))
            throw new BadRequestException(localizer.Get(ResourceKeys.GroupNameExists), "GROUP_NAME_EXISTS");

        var newRoleNames = await registrar.ResolveRoleNamesAsync(request.RoleIds, cancellationToken);
        await GroupAdminGuard.CheckGroupChangeAsync(userManager, resolver, db, currentUser, localizer,
            group.Id, newRoleNames, cancellationToken);

        group.Rename(name, request.Description);
        var rolesRemoved = group.SetRoles(request.RoleIds);
        await db.SaveChangesAsync(cancellationToken);

        if (rolesRemoved)
            await resolver.InvalidateTokensAsync(await UserGroupQueries.MemberIdsAsync(db, group.Id, cancellationToken),
                exceptUserId: currentUser.UserId);

        return await sender.Send(new Queries.GetUserGroupQuery(group.Id), cancellationToken);
    }
}

// ---- Delete ----------------------------------------------------------------------------------------------

public sealed record DeleteUserGroupCommand(Guid Id) : ICommand<bool>;

public sealed class DeleteUserGroupHandler(
    IAppDbContext db,
    UserManager<ApplicationUser> userManager,
    UserRoleResolver resolver,
    ICurrentUserService currentUser,
    IStringLocalizerService localizer) : ICommandHandler<DeleteUserGroupCommand, bool>
{
    public async Task<bool> Handle(DeleteUserGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await db.UserGroups.Include(g => g.Roles).FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken)
                    ?? throw new NotFoundException(localizer.Get(ResourceKeys.GroupNotFound));

        await GroupAdminGuard.CheckGroupChangeAsync(userManager, resolver, db, currentUser, localizer,
            group.Id, newGroupRoles: [], cancellationToken);

        var members = await UserGroupQueries.MemberIdsAsync(db, group.Id, cancellationToken);
        var hadRoles = group.Roles.Count > 0;

        db.UserGroupMembers.RemoveRange(db.UserGroupMembers.Where(m => m.GroupId == group.Id));
        db.UserGroups.Remove(group);
        await db.SaveChangesAsync(cancellationToken);

        if (hadRoles)
            await resolver.InvalidateTokensAsync(members, exceptUserId: currentUser.UserId);
        return true;
    }
}

// ---- A user's groups -------------------------------------------------------------------------------------

/// <summary>Puts the user in exactly these groups. Leaving a group that gave roles signs the user out.</summary>
public sealed record SetUserGroupsCommand(Guid UserId, List<Guid> GroupIds) : ICommand<List<UserGroupListItemDto>>;

public sealed class SetUserGroupsValidator : AbstractValidator<SetUserGroupsCommand>
{
    public SetUserGroupsValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.GroupIds).NotNull();
        RuleForEach(x => x.GroupIds).NotEmpty();
    }
}

public sealed class SetUserGroupsHandler(
    IAppDbContext db,
    UserManager<ApplicationUser> userManager,
    UserRoleResolver resolver,
    ICurrentUserService currentUser,
    ISender sender,
    IStringLocalizerService localizer,
    TimeProvider clock) : ICommandHandler<SetUserGroupsCommand, List<UserGroupListItemDto>>
{
    public async Task<List<UserGroupListItemDto>> Handle(SetUserGroupsCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString())
                   ?? throw new NotFoundException(localizer.Get(ResourceKeys.UserNotFound));

        var wanted = request.GroupIds.Distinct().ToList();
        if (await db.UserGroups.CountAsync(g => wanted.Contains(g.Id), cancellationToken) != wanted.Count)
            throw new BadRequestException(localizer.Get(ResourceKeys.GroupNotFound));

        var current = await db.UserGroupMembers.Where(m => m.UserId == user.Id).ToListAsync(cancellationToken);
        var toRemove = current.Where(m => !wanted.Contains(m.GroupId)).ToList();
        var toAdd = wanted.Where(id => current.All(m => m.GroupId != id)).ToList();

        // Own account: the new set of groups (plus direct roles) must still give Administrator.
        var isSelf = currentUser.UserId == user.Id;
        if (isSelf && toRemove.Count > 0)
        {
            var before = await resolver.GetEffectiveRolesAsync(user, cancellationToken);
            var newGroupRoles = await (from gr in db.UserGroupRoles
                                       join r in db.Roles on gr.RoleId equals r.Id
                                       where wanted.Contains(gr.GroupId)
                                       select r.Name!).ToListAsync(cancellationToken);
            GroupAdminGuard.EnsureStillAdministrator(before, await userManager.GetRolesAsync(user), newGroupRoles, localizer);
        }

        var removedGroupIds = toRemove.Select(m => m.GroupId).ToList();
        var lostRoles = removedGroupIds.Count > 0 &&
                        await db.UserGroupRoles.AnyAsync(gr => removedGroupIds.Contains(gr.GroupId), cancellationToken);

        db.UserGroupMembers.RemoveRange(toRemove);
        var now = clock.GetUtcNow().UtcDateTime;
        foreach (var groupId in toAdd)
            db.UserGroupMembers.Add(UserGroupMember.Create(groupId, user.Id, now));
        await db.SaveChangesAsync(cancellationToken);

        if (lostRoles && !isSelf)
            await resolver.InvalidateTokensAsync([user.Id], exceptUserId: currentUser.UserId);

        return await sender.Send(new Queries.GetGroupsOfUserQuery(user.Id), cancellationToken);
    }
}
