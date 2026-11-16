using ServicesA.Application.Features.Identity.Account.Commands.RefreshSession;

namespace ServicesA.API.Endpoints.Identity.Account.Command;

public class RefreshSession : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        // Needs the current (still valid) token: an expired or ended session must sign in again.
        app.MapPost($"{UrlTemplates.AccountRoute}/refresh-session",
                async (ISender sender, CancellationToken token) =>
                    Results.Ok(await sender.Send(new RefreshSessionCommand(), token)))
            .WithTags("Account")
            .RequireAuthorization()
            .Produces<AuthResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
