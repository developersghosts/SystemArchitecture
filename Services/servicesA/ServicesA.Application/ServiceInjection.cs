using System.Reflection;

namespace ServicesA.Application;

public static class ServiceInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(ServiceInjection).Assembly;

        // MediatR
        services.AddMediatR(cfg => cfg
            .RegisterServicesFromAssembly(assembly)
            .AddBuildingBlocksBehaviors()); // LoggingBehavior + ValidationBehavior

        // FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // Mapster
        services.AddMapster(assembly);

        // Identity feature helpers
        services.AddScoped<Features.Identity.Common.UserRegistrar>();
        services.AddScoped<Features.Identity.Common.LoginHistoryRecorder>();
        services.AddScoped<Features.Identity.Common.UserRoleResolver>();
        services.AddScoped<Features.Settings.EmailSenderNameProvider>();
        services.AddScoped<Features.Settings.EmailBrandingProvider>();
        services.AddScoped<Features.Identity.Common.EmailTemplates>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }

    private static IServiceCollection AddMapster(this IServiceCollection services, Assembly assembly)
    {
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(assembly);

        services.AddSingleton(config);
        services.AddScoped<MapsterMapper.IMapper, MapsterMapper.ServiceMapper>();

        return services;
    }
}
