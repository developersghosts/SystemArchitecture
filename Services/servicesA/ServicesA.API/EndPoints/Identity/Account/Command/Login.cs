using ServicesA.Application.Features.Identity.Account.Commands.Login;

namespace ServicesA.API.Endpoints.Identity.Account.Command;

/// <summary>Step 1: user name + password. Emails a 6-digit code; then call verify-otp.</summary>
public class Login : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost($"{UrlTemplates.AccountRoute}/login",
                async (LoginCommand command, ISender sender, CancellationToken token) =>
                    Results.Ok(await sender.Send(command, token)))
            .WithTags("Account")
            .AllowAnonymous()
            .Produces<LoginResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting(AuthRateLimiter.PolicyName);
    }
}
