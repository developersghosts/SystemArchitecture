using ServicesA.Domain.Entities.Settings;

namespace ServicesA.Application.Features.Settings.Queries.GetProjectConfiguration;

/// <summary>Returns the saved configuration, or the defaults when nothing was saved yet.</summary>
public sealed record GetProjectConfigurationQuery : IQuery<ProjectConfigurationDto>;

public sealed class GetProjectConfigurationHandler(IAppDbContext db)
    : IQueryHandler<GetProjectConfigurationQuery, ProjectConfigurationDto>
{
    public async Task<ProjectConfigurationDto> Handle(GetProjectConfigurationQuery request,
        CancellationToken cancellationToken)
    {
        var configuration = await db.ProjectConfigurations.AsNoTracking()
            .OrderBy(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return (configuration ?? ProjectConfiguration.CreateDefault()).ToDto();
    }
}
