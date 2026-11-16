using ServicesA.Application.Features.Identity.Account.Commands.VerifyOtp;

namespace ServicesA.API.Endpoints.Identity.Account.Command;

/// <summary>Step 2: user name + password + code. Returns the JWT.</summary>
public class VerifyOtp : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost($"{UrlTemplates.AccountRoute}/verify-otp",
                async (VerifyOtpCommand command, ISender sender, CancellationToken token) =>
                    Results.Ok(await sender.Send(command, token)))
            .WithTags("Account")
            .AllowAnonymous()
            .Produces<AuthResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting(AuthRateLimiter.PolicyName);
    }
}
