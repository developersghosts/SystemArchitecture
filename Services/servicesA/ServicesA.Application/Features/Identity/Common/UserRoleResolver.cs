namespace ServicesA.Application.Features.Identity.Common;

/// <summary>
/// A user's effective roles = roles given directly (AspNetUserRoles) + roles of every group the user is in.
/// The JWT carries the effective roles, so [RequireRole] works the same for both.
/// </summary>
public sealed class UserRoleResolver(UserManager<ApplicationUser> userManager, IAppDbContext db)
{
    public async Task<List<string>> GetEffectiveRolesAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var direct = await userManager.GetRolesAsync(user);
        var fromGroups = await GroupRoleNamesAsync(user.Id, excludeGroupId: null, cancellationToken);
        return direct.Concat(fromGroups).Distinct().Order().ToList();
    }

    /// <summary>Role names the user gets from groups, optionally ignoring one group (to test a change before saving it).</summary>
    public Task<List<string>> GroupRoleNamesAsync(Guid userId, Guid? excludeGroupId, CancellationToken cancellationToken) =>
        (from m in db.UserGroupMembers
         join gr in db.UserGroupRoles on m.GroupId equals gr.GroupId
         join r in db.Roles on gr.RoleId equals r.Id
         where m.UserId == userId && (excludeGroupId == null || m.GroupId != excludeGroupId)
         select r.Name!)
        .Distinct()
        .ToListAsync(cancellationToken);

    public async Task<bool> HasDirectRoleAsync(ApplicationUser user, string role) =>
        await userManager.IsInRoleAsync(user, role);

    /// <summary>
    /// Makes the current tokens of these users invalid (they sign in again and get their new roles).
    /// The caller's own token is kept, so an admin editing a group they belong to is not signed out.
    /// </summary>
    public async Task InvalidateTokensAsync(IEnumerable<Guid> userIds, Guid? exceptUserId)
    {
        foreach (var id in userIds.Distinct().Where(id => id != exceptUserId))
        {
            var user = await userManager.FindByIdAsync(id.ToString());
            if (user is not null)
                (await userManager.UpdateSecurityStampAsync(user)).EnsureSucceeded();
        }
    }
}
