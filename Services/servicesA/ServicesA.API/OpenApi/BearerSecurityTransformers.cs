using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ServicesA.API.OpenApi;

/// <summary>Adds the "Bearer" JWT scheme so Swagger UI shows an Authorize button.</summary>
internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public const string SchemeName = "Bearer";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info.Title = "ServicesA API";
        document.Info.Description = "Login: POST /api/Account/login, then POST /api/Account/verify-otp, then paste the token in Authorize.";

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Paste the token returned by verify-otp (without the word Bearer)."
        };

        return Task.CompletedTask;
    }
}

/// <summary>Marks only the endpoints that require authorization with the lock icon.</summary>
internal sealed class BearerSecurityRequirementTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        var requiresAuth = metadata.Any(m => m is IAuthorizeData or AuthorizationPolicy) &&
                           !metadata.Any(m => m is IAllowAnonymous);

        if (requiresAuth)
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(BearerSecuritySchemeTransformer.SchemeName, context.Document)] = []
            });
        }

        return Task.CompletedTask;
    }
}
