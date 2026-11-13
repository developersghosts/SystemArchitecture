using System.Reflection;

namespace ServicesA.Application;

public static class ServiceInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(ServiceInjection).Assembly;

        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // Mapster
        services.AddMapster(assembly);

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
