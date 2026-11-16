using ServicesA.Application.Features.Identity.UserRoles.Queries.GetUserRoles;

namespace ServicesA.API.Endpoints.Identity.UserRoles.Query;

public class GetAllUserRolesListByIdApi : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet($"{UrlTemplates.UserRolesRoute}/getAllUserRolesList/{{userId:guid}}",
                async (Guid userId, ISender sender, CancellationToken token) =>
                    Results.Ok(await sender.Send(new GetUserRolesQuery(userId), token)))
            .WithTags("UserRole")
            .RequireAuthorization(p => p.RequireRole(IdentityRoles.GetAllRolesList, IdentityRoles.Administrator))
            .Produces<List<UserRoleDto>>();
    }
}
