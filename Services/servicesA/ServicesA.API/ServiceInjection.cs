namespace ServicesA.API;

public static class ServiceInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        // Carter
        services.AddCarter();
        services.AddOpenApi();

        return services;
    }
}
