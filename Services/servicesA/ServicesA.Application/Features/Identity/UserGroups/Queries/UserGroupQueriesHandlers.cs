namespace ServicesA.Application.Features.Identity.UserGroups.Queries;

/// <summary>Paged list of groups with their roles and member count. FindValues searches name and description.</summary>
public sealed record GetUserGroupsQuery(PaginationParams Params) : IQuery<PagedList<UserGroupListItemDto>>;

public sealed class GetUserGroupsHandler(IAppDbContext db) : IQueryHandler<GetUserGroupsQuery, PagedList<UserGroupListItemDto>>
{
    public Task<PagedList<UserGroupListItemDto>> Handle(GetUserGroupsQuery request, CancellationToken cancellationToken)
    {
        var groups = db.UserGroups.AsNoTracking();
        var keyword = request.Params.FindValues?.Trim();
        if (!string.IsNullOrEmpty(keyword))
            groups = groups.Where(g => g.Name.Contains(keyword) || (g.Description != null && g.Description.Contains(keyword)));

        return PagedList<UserGroupListItemDto>.CreateAsync(
            UserGroupQueries.ListItems(db, groups.OrderBy(g => g.Name)),
            request.Params.Page, request.Params.Size, cancellationToken);
    }
}

/// <summary>One group with its roles and members.</summary>
public sealed record GetUserGroupQuery(Guid Id) : IQuery<UserGroupDetailsDto>;

public sealed class GetUserGroupHandler(IAppDbContext db, IStringLocalizerService localizer)
    : IQueryHandler<GetUserGroupQuery, UserGroupDetailsDto>
{
    public async Task<UserGroupDetailsDto> Handle(GetUserGroupQuery request, CancellationToken cancellationToken)
    {
        var entity = await db.UserGroups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken)
                     ?? throw new NotFoundException(localizer.Get(ResourceKeys.GroupNotFound));
        var groupId = entity.Id;

        var roles = await UserGroupQueries.RolesOf(db, groupId).ToListAsync(cancellationToken);
        var members = await (from m in db.UserGroupMembers
                             join u in db.Users on m.UserId equals u.Id
                             where m.GroupId == groupId
                             orderby u.UserName
                             select new GroupMemberDto(u.Id, u.UserName!, u.FullName.FirstName + " " + u.FullName.LastName, u.Email!))
            .ToListAsync(cancellationToken);

        return new UserGroupDetailsDto(entity.Id, entity.Name, entity.Description, roles, members,
            entity.CreatedAtUtc, entity.ModifiedAtUtc);
    }
}

/// <summary>The groups a user belongs to.</summary>
public sealed record GetGroupsOfUserQuery(Guid UserId) : IQuery<List<UserGroupListItemDto>>;

public sealed class GetGroupsOfUserHandler(IAppDbContext db) : IQueryHandler<GetGroupsOfUserQuery, List<UserGroupListItemDto>>
{
    public Task<List<UserGroupListItemDto>> Handle(GetGroupsOfUserQuery request, CancellationToken cancellationToken)
    {
        var groups = db.UserGroups.AsNoTracking()
            .Where(g => db.UserGroupMembers.Any(m => m.GroupId == g.Id && m.UserId == request.UserId))
            .OrderBy(g => g.Name);
        return UserGroupQueries.ListItems(db, groups).ToListAsync(cancellationToken);
    }
}
