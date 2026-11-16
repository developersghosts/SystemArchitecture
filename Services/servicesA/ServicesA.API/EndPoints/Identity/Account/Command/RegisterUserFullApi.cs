using ServicesA.Application.Features.Identity.Account.Commands.RegisterUserFull;

namespace ServicesA.API.Endpoints.Identity.Account.Command;

public class RegisterUserFullApi : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost($"{UrlTemplates.AccountRoute}/registerUserFull",
                async (RegisterUserFullCommand command, ISender sender, CancellationToken token) =>
                {
                    var userId = await sender.Send(command, token);
                    return Results.Created($"{UrlTemplates.AccountRoute}/getAllUserList?findValues={userId}", userId);
                })
            .WithTags("Account")
            .RequireAuthorization(p => p.RequireRole(IdentityRoles.Administrator))
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
