using ServicesA.Application.Features.Identity.UserRoles.Commands.SetUserRoles;

namespace ServicesA.API.Endpoints.Identity.UserRoles.Command;

public class SetUserRolesApi : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        // Removing roles is more powerful than adding them, so Administrator only.
        app.MapPut($"{UrlTemplates.UserRolesRoute}/setUserRoles",
                async (SetUserRolesCommand command, ISender sender, CancellationToken token) =>
                    Results.Ok(await sender.Send(command, token)))
            .WithTags("UserRole")
            .RequireAuthorization(p => p.RequireRole(IdentityRoles.Administrator))
            .Produces<List<UserRoleDto>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
