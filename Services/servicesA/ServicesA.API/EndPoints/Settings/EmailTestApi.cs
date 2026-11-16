using ServicesA.Application.Abstractions;
using ServicesA.Application.Features.Settings.Email;

namespace ServicesA.API.Endpoints.Settings;

/// <summary>Email server settings saved in the database, and "send a test email" (Administrator).</summary>
public class EmailTestApi : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet($"{UrlTemplates.ConfigurationRoute}/email-settings",
                async (ISender sender, CancellationToken token) =>
                    Results.Ok(await sender.Send(new GetEmailServerSettingsQuery(), token)))
            .WithTags("Configuration")
            .RequireAuthorization(p => p.RequireRole(IdentityRoles.Administrator))
            .Produces<EmailServerSettingsDto>();

        app.MapPut($"{UrlTemplates.ConfigurationRoute}/email-settings",
                async (UpdateEmailServerSettingsCommand command, ISender sender, CancellationToken token) =>
                    Results.Ok(await sender.Send(command, token)))
            .WithTags("Configuration")
            .RequireAuthorization(p => p.RequireRole(IdentityRoles.Administrator))
            .Produces<EmailServerSettingsDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        app.MapPost($"{UrlTemplates.ConfigurationRoute}/test-email",
                async (SendTestEmailCommand command, ISender sender, CancellationToken token) =>
                    Results.Ok(await sender.Send(command, token)))
            .WithTags("Configuration")
            .RequireAuthorization(p => p.RequireRole(IdentityRoles.Administrator))
            .RequireRateLimiting(AuthRateLimiter.PolicyName) // stops it being used to spam
            .Produces<EmailDeliveryResult>()
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
