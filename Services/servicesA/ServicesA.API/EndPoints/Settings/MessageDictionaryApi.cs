using ServicesA.Application.Features.Settings.Localization;

namespace ServicesA.API.Endpoints.Settings;

/// <summary>Message dictionary (English / Egyptian / Saudi) saved in the database. Administrator only.</summary>
public class MessageDictionaryApi : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet($"{UrlTemplates.ConfigurationRoute}/messages",
                async (string? search, ISender sender, CancellationToken token) =>
                    Results.Ok(await sender.Send(new GetMessageDictionaryQuery(search), token)))
            .WithTags("Configuration")
            .RequireAuthorization(p => p.RequireRole(IdentityRoles.Administrator))
            .Produces<MessageDictionaryDto>();

        app.MapPut($"{UrlTemplates.ConfigurationRoute}/messages",
                async (UpdateMessageTextCommand command, ISender sender, CancellationToken token) =>
                    Results.Ok(await sender.Send(command, token)))
            .WithTags("Configuration")
            .RequireAuthorization(p => p.RequireRole(IdentityRoles.Administrator))
            .Produces<MessageTextDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
