using ServicesA.Application.Features.Identity.Common;

namespace ServicesA.Application.Features.Identity.UserRoles.Queries.GetUserRoles;

public sealed record GetUserRolesQuery(Guid UserId) : IQuery<List<UserRoleDto>>;

public sealed class GetUserRolesHandler(IAppDbContext db) : IQueryHandler<GetUserRolesQuery, List<UserRoleDto>>
{
    public Task<List<UserRoleDto>> Handle(GetUserRolesQuery request, CancellationToken cancellationToken) =>
        (from ur in db.UserRoles.AsNoTracking()
         join u in db.Users on ur.UserId equals u.Id
         join r in db.Roles on ur.RoleId equals r.Id
         where ur.UserId == request.UserId
         orderby r.Name
         select new UserRoleDto(u.Id, u.UserName!, u.FullName.FirstName + " " + u.FullName.LastName,
             r.Id, r.Name!, r.Title))
        .ToListAsync(cancellationToken);
}
