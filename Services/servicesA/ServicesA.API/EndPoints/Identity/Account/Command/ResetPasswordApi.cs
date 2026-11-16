using ServicesA.Application.Features.Identity.Account.Commands.ResetPassword;

namespace ServicesA.API.Endpoints.Identity.Account.Command;

public class ResetPasswordApi : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost($"{UrlTemplates.AccountRoute}/reset-password",
                async (ResetPasswordCommand command, ISender sender, CancellationToken token) =>
                    Results.Ok(await sender.Send(command, token)))
            .WithTags("Account")
            .AllowAnonymous()
            .Produces<bool>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting(AuthRateLimiter.PolicyName);
    }
}
