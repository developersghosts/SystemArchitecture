using ServicesA.Application.Features.Identity.Account.Commands.Logout;

namespace ServicesA.API.Endpoints.Identity.Account.Command;

public class Logout : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost($"{UrlTemplates.AccountRoute}/logout",
                async (ISender sender, CancellationToken token) =>
                {
                    await sender.Send(new LogoutCommand(), token);
                    return Results.Ok();
                })
            .WithTags("Account")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
