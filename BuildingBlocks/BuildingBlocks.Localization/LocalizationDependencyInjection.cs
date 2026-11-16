using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Localization;

public static class LocalizationDependencyInjection
{
    public static IServiceCollection AddLocalizationDependencyInjection(this IServiceCollection services)
    {
        // Resources live next to SharedResources: Resources/SharedResources.{culture}.resx
        services.AddLocalization();

        services.AddHttpContextAccessor();
        services.AddScoped<ILanguageService, LanguageService>();
        services.AddScoped<IStringLocalizerService, StringLocalizerService>();

        return services;
    }
}
