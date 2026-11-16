using ServicesA.Domain.Common;

namespace ServicesA.Domain.Entities.Identity;

/// <summary>
/// A named set of roles. Users can belong to several groups; a user's roles are their own roles
/// plus the roles of every group they are in (see UserRoleResolver in Application).
/// </summary>
public sealed class UserGroup : AggregateRoot<Guid>, IAuditableEntity
{
    private readonly List<UserGroupRole> _roles = [];

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public IReadOnlyCollection<UserGroupRole> Roles => _roles.AsReadOnly();

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ModifiedAtUtc { get; private set; }

    private UserGroup() { }

    public static UserGroup Create(string name, string? description, IEnumerable<Guid> roleIds)
    {
        var group = new UserGroup { Id = Guid.CreateVersion7() };
        group.Rename(name, description);
        group.SetRoles(roleIds);
        return group;
    }

    public void Rename(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>Replaces the roles. Returns true when at least one role was removed (members must sign in again).</summary>
    public bool SetRoles(IEnumerable<Guid> roleIds)
    {
        var wanted = roleIds.Distinct().ToHashSet();
        var removed = _roles.RemoveAll(r => !wanted.Contains(r.RoleId)) > 0;
        foreach (var roleId in wanted.Where(id => _roles.All(r => r.RoleId != id)))
            _roles.Add(new UserGroupRole(Id, roleId));
        return removed;
    }

    public void SetCreated(DateTime utcNow) => CreatedAtUtc = utcNow;

    public void SetModified(DateTime utcNow) => ModifiedAtUtc = utcNow;
}

/// <summary>A role inside a group (part of the UserGroup aggregate).</summary>
public sealed class UserGroupRole
{
    public Guid GroupId { get; private set; }
    public Guid RoleId { get; private set; }

    private UserGroupRole() { }

    internal UserGroupRole(Guid groupId, Guid roleId)
    {
        GroupId = groupId;
        RoleId = roleId;
    }
}

/// <summary>Membership of a user in a group.</summary>
public sealed class UserGroupMember
{
    public Guid GroupId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime AddedAtUtc { get; private set; }

    private UserGroupMember() { }

    public static UserGroupMember Create(Guid groupId, Guid userId, DateTime utcNow) =>
        new() { GroupId = groupId, UserId = userId, AddedAtUtc = utcNow };
}
