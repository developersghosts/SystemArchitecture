using ServicesA.Application.Features.Identity.Roles.Queries.GetRoleList;

namespace ServicesA.API.Endpoints.Identity.Roles.Query;

public class GetRoleListApi : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet($"{UrlTemplates.RoleRoute}/getAllRolesList",
                async ([AsParameters] PaginationParams paging, ISender sender, HttpContext context, CancellationToken token) =>
                {
                    var roles = await sender.Send(new GetRoleListQuery(paging), token);
                    context.Response.AddPagination(roles);
                    return Results.Ok(new { items = roles, totalCount = roles.TotalCount, totalPages = roles.TotalPages });
                })
            .WithTags("Role")
            .RequireAuthorization(p => p.RequireRole(IdentityRoles.GetAllRolesList, IdentityRoles.Administrator))
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
