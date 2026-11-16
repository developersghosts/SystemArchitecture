using ServicesA.Application.Features.Identity.Common;

namespace ServicesA.Application.Features.Identity.UserGroups;

public sealed record GroupRoleDto(Guid Id, string Name, string Title);

public sealed record GroupMemberDto(Guid UserId, string UserName, string FullName, string Email);

public sealed record UserGroupListItemDto(
    Guid Id,
    string Name,
    string? Description,
    int MemberCount,
    List<GroupRoleDto> Roles);

public sealed record UserGroupDetailsDto(
    Guid Id,
    string Name,
    string? Description,
    List<GroupRoleDto> Roles,
    List<GroupMemberDto> Members,
    DateTime CreatedAtUtc,
    DateTime? ModifiedAtUtc);

internal static class UserGroupQueries
{
    public static IQueryable<GroupRoleDto> RolesOf(IAppDbContext db, Guid groupId) =>
        from gr in db.UserGroupRoles
        join r in db.Roles on gr.RoleId equals r.Id
        where gr.GroupId == groupId
        orderby r.Name
        select new GroupRoleDto(r.Id, r.Name!, r.Title);

    public static IQueryable<UserGroupListItemDto> ListItems(IAppDbContext db, IQueryable<UserGroup> groups) =>
        groups.Select(g => new UserGroupListItemDto(
            g.Id,
            g.Name,
            g.Description,
            db.UserGroupMembers.Count(m => m.GroupId == g.Id),
            (from gr in db.UserGroupRoles
             join r in db.Roles on gr.RoleId equals r.Id
             where gr.GroupId == g.Id
             orderby r.Name
             select new GroupRoleDto(r.Id, r.Name!, r.Title)).ToList()));

    public static Task<List<Guid>> MemberIdsAsync(IAppDbContext db, Guid groupId, CancellationToken ct) =>
        db.UserGroupMembers.Where(m => m.GroupId == groupId).Select(m => m.UserId).ToListAsync(ct);
}

/// <summary>Stops a change to groups that would take the Administrator role away from the caller.</summary>
internal static class GroupAdminGuard
{
    /// <param name="directAndOtherGroupRoles">The caller's roles that do not come from the changed group.</param>
    /// <param name="newGroupRoles">The changed group's roles after the change (empty when deleted or left).</param>
    public static void EnsureStillAdministrator(IReadOnlyCollection<string> rolesBefore,
        IEnumerable<string> directAndOtherGroupRoles, IEnumerable<string> newGroupRoles, IStringLocalizerService localizer)
    {
        if (!rolesBefore.Contains(IdentityRoles.Administrator)) return;
        if (directAndOtherGroupRoles.Contains(IdentityRoles.Administrator) || newGroupRoles.Contains(IdentityRoles.Administrator))
            return;
        throw new BadRequestException(localizer.Get(ResourceKeys.CannotLoseOwnAdministratorByGroup), "CANNOT_REMOVE_OWN_ADMIN");
    }

    /// <summary>For create/update/delete of one group: checks the caller only when they are a member of it.</summary>
    public static async Task CheckGroupChangeAsync(UserManager<ApplicationUser> userManager, UserRoleResolver resolver,
        IAppDbContext db, ICurrentUserService currentUser, IStringLocalizerService localizer,
        Guid groupId, IReadOnlyCollection<string> newGroupRoles, CancellationToken ct)
    {
        if (currentUser.UserId is not { } me) return;
        if (!await db.UserGroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == me, ct)) return;

        var user = await userManager.FindByIdAsync(me.ToString());
        if (user is null) return;

        var before = await resolver.GetEffectiveRolesAsync(user, ct);
        var direct = await userManager.GetRolesAsync(user);
        var otherGroups = await resolver.GroupRoleNamesAsync(me, excludeGroupId: groupId, ct);
        EnsureStillAdministrator(before, direct.Concat(otherGroups).ToList(), newGroupRoles, localizer);
    }
}
