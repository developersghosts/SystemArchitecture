using ServicesA.Application.Features.Identity.Common;

namespace ServicesA.Application.Features.Identity.Account.Queries.GetAllUsers;

public sealed record GetAllUsersQuery(PaginationParams Params) : IQuery<PagedList<UserListItemDto>>;

public sealed class GetAllUsersHandler(IAppDbContext db) : IQueryHandler<GetAllUsersQuery, PagedList<UserListItemDto>>
{
    public Task<PagedList<UserListItemDto>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        var query = db.Users.AsNoTracking();

        var keyword = request.Params.FindValues?.Trim();
        if (!string.IsNullOrEmpty(keyword))
        {
            query = Guid.TryParse(keyword, out var id)
                ? query.Where(u => u.Id == id)
                : query.Where(u =>
                    u.UserName!.Contains(keyword) ||
                    u.Email!.Contains(keyword) ||
                    u.FullName.FirstName.Contains(keyword) ||
                    u.FullName.LastName.Contains(keyword) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(keyword)));
        }

        var projection = query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Select(u => new UserListItemDto(
                u.Id, u.UserName!, u.FullName.FirstName, u.FullName.LastName, u.Email!, u.PhoneNumber,
                u.DateOfBirth, u.IsActive, u.CreatedAtUtc, u.LastActiveAtUtc));

        return PagedList<UserListItemDto>.CreateAsync(projection, request.Params.Page, request.Params.Size,
            cancellationToken);
    }
}
