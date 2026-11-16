using ServicesA.Application.Features.Identity.Account.Queries.GetAllUsers;

namespace ServicesA.API.Endpoints.Identity.Account.Query;

public class GetAllUsersApi : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet($"{UrlTemplates.AccountRoute}/getAllUserList",
                async ([AsParameters] PaginationParams paging, ISender sender, HttpContext context, CancellationToken token) =>
                {
                    var users = await sender.Send(new GetAllUsersQuery(paging), token);
                    context.Response.AddPagination(users);
                    return Results.Ok(new { items = users, totalCount = users.TotalCount, totalPages = users.TotalPages });
                })
            .WithTags("Account")
            .RequireAuthorization(p => p.RequireRole(IdentityRoles.GetAllUsers, IdentityRoles.Administrator))
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
