using System.Globalization;
using ServicesA.Infrastructure.Persistence;

namespace ServicesA.API;

public static class ServiceInjection
{
    public const string UiCorsPolicy = "UiCors";

    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        // CORS for the Angular UI; origins come from "Cors:AllowedOrigins"
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(options => options.AddPolicy(UiCorsPolicy, policy => policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("Pagination", "Retry-After")));

        // Carter
        services.AddCarter();
        // OpenAPI document (/openapi/v1.json) with JWT support; Swagger UI is mapped in Program.cs
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<OpenApi.BearerSecuritySchemeTransformer>();
            options.AddOperationTransformer<OpenApi.BearerSecurityRequirementTransformer>();
        });

        // BuildingBlocks: exceptions -> ProblemDetails, and Accept-Language based localization
        services.AddCustomExceptionHandler();
        services.AddLocalizationDependencyInjection();

        // Sets CurrentUICulture per request from Accept-Language (also localizes FluentValidation messages)
        services.Configure<RequestLocalizationOptions>(options =>
        {
            var cultures = LocalizationConstants.SupportedLanguages.Select(c => new CultureInfo(c)).ToList();
            options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(LocalizationConstants.DefaultLanguage);
            options.SupportedCultures = cultures;
            options.SupportedUICultures = cultures;
        });

        // Auth
        services.AddAuthorization();
        services.AddAuthRateLimiter();

        return services;
    }

    public static IHostBuilder AddSerilogLogging(this IHostBuilder host)
    {
        // Serilog (configured from the "Serilog" section in appsettings)
        return host.UseSerilog((context, configuration) =>
            configuration.ReadFrom.Configuration(context.Configuration));
    }

    /// <summary>Applies migrations and seeds roles + admin when "Database:InitializeOnStartup" is true.</summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        if (app.Configuration.GetValue<bool>("Database:InitializeOnStartup"))
            await DatabaseInitializer.InitializeAsync(app.Services);
    }
}
