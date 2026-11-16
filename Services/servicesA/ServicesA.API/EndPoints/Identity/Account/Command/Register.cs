using ServicesA.Application.Features.Identity.Account.Commands.Register;

namespace ServicesA.API.Endpoints.Identity.Account.Command;

public class Register : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost($"{UrlTemplates.AccountRoute}/register",
                async (RegisterCommand command, ISender sender, CancellationToken token) =>
                {
                    var userId = await sender.Send(command, token);
                    return Results.Created($"{UrlTemplates.AccountRoute}/getAllUserList?findValues={userId}", userId);
                })
            .WithTags("Account")
            .RequireAuthorization(p => p.RequireRole(IdentityRoles.RegisterUser, IdentityRoles.Administrator))
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
