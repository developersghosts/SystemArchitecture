using ServicesA.Application.Features.Settings;
using ServicesA.Application.Features.Settings.Commands.UpdateProjectConfiguration;
using ServicesA.Application.Features.Settings.Queries.GetProjectConfiguration;

namespace ServicesA.API.Endpoints.Settings;

public class ProjectConfigurationApi : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        // Public: the UI needs the company name, colors and fonts before login.
        app.MapGet(UrlTemplates.ConfigurationRoute,
                async (ISender sender, CancellationToken token) =>
                    Results.Ok(await sender.Send(new GetProjectConfigurationQuery(), token)))
            .WithTags("Configuration")
            .AllowAnonymous()
            .Produces<ProjectConfigurationDto>();

        app.MapPut(UrlTemplates.ConfigurationRoute,
                async (UpdateProjectConfigurationCommand command, ISender sender, CancellationToken token) =>
                    Results.Ok(await sender.Send(command, token)))
            .WithTags("Configuration")
            .RequireAuthorization(p => p.RequireRole(IdentityRoles.Administrator))
            .Produces<ProjectConfigurationDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
