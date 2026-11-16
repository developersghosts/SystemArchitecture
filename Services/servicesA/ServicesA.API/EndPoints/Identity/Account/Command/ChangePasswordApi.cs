using ServicesA.Application.Features.Identity.Account.Commands.ChangePassword;

namespace ServicesA.API.Endpoints.Identity.Account.Command;

/// <summary>Changes the caller's password and ends all sessions (log in again afterwards).</summary>
public class ChangePasswordApi : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost($"{UrlTemplates.AccountRoute}/change-password",
                async (ChangePasswordCommand command, ISender sender, CancellationToken token) =>
                    Results.Ok(await sender.Send(command, token)))
            .WithTags("Account")
            .RequireAuthorization()
            .Produces<bool>()
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
