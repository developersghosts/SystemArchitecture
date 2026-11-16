using BuildingBlocks.Behaviors;
using BuildingBlocks.Exceptions.Handler;

namespace BuildingBlocks;

public static class DependencyInjection
{
    /// <summary>Adds the Logging and Validation pipeline behaviors to MediatR.</summary>
    public static MediatRServiceConfiguration AddBuildingBlocksBehaviors(this MediatRServiceConfiguration config)
    {
        config.AddOpenBehavior(typeof(LoggingBehavior<,>));
        config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        return config;
    }

    /// <summary>Registers CustomExceptionHandler and ProblemDetails. Call app.UseExceptionHandler() in the pipeline.</summary>
    public static IServiceCollection AddCustomExceptionHandler(this IServiceCollection services)
    {
        services.AddExceptionHandler<CustomExceptionHandler>();
        services.AddProblemDetails();
        return services;
    }
}
