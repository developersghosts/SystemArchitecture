using ServicesA.Application.Features.Identity.UserRoles.Commands.CreateUserRole;

namespace ServicesA.API.Endpoints.Identity.UserRoles.Command;

public class CreateUserRoleApi : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost($"{UrlTemplates.UserRolesRoute}/createUserRole",
                async (CreateUserRoleCommand command, ISender sender, CancellationToken token) =>
                {
                    var roleIds = await sender.Send(command, token);
                    return Results.Created($"{UrlTemplates.UserRolesRoute}/getAllUserRolesList/{command.UserId}", roleIds);
                })
            .WithTags("UserRole")
            .RequireAuthorization(p => p.RequireRole(IdentityRoles.CreateUserRole, IdentityRoles.Administrator))
            .Produces<List<Guid>>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
