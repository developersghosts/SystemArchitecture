using ServicesA.Application.Features.Identity.Common;

namespace ServicesA.Application.Features.Identity.Roles.Queries.GetRoleList;

public sealed record GetRoleListQuery(PaginationParams Params) : IQuery<PagedList<RoleListDto>>;

public sealed class GetRoleListHandler(IAppDbContext db) : IQueryHandler<GetRoleListQuery, PagedList<RoleListDto>>
{
    public Task<PagedList<RoleListDto>> Handle(GetRoleListQuery request, CancellationToken cancellationToken)
    {
        var query = db.Roles.AsNoTracking();

        var keyword = request.Params.FindValues?.Trim();
        if (!string.IsNullOrEmpty(keyword))
        {
            query = Guid.TryParse(keyword, out var id)
                ? query.Where(r => r.Id == id)
                : query.Where(r => r.Name!.Contains(keyword) || r.Title.Contains(keyword));
        }

        var projection = query
            .OrderBy(r => r.Name)
            .Select(r => new RoleListDto(r.Id, r.Name!, r.Title, r.Description));

        return PagedList<RoleListDto>.CreateAsync(projection, request.Params.Page, request.Params.Size,
            cancellationToken);
    }
}
