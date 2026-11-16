using ServicesA.Application.Features.Identity.Roles.Commands.CreateRole;

namespace ServicesA.API.Endpoints.Identity.Roles.Command;

public class CreateRoleApi : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost($"{UrlTemplates.RoleRoute}/createRole",
                async (CreateRoleCommand command, ISender sender, CancellationToken token) =>
                {
                    var created = await sender.Send(command, token);
                    return Results.Created($"{UrlTemplates.RoleRoute}/getAllRolesList", created);
                })
            .WithTags("Role")
            .RequireAuthorization(p => p.RequireRole(IdentityRoles.CreateRole, IdentityRoles.Administrator))
            .Produces<List<string>>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
