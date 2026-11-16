using System.Threading.RateLimiting;
using BuildingBlocks.Localization.Resources;

namespace ServicesA.API;

/// <summary>
/// Rate-limit policy for the anonymous auth endpoints (login, verify-otp, resend-otp, forgot/reset password):
/// 5 requests per 30 seconds, counted per client IP and per endpoint. Rejections return 429 with Retry-After.
/// </summary>
public static class AuthRateLimiter
{
    public const string PolicyName = "AuthLimiter";

    public static IServiceCollection AddAuthRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, token) =>
            {
                double? retryAfterSeconds = null;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    retryAfterSeconds = retryAfter.TotalSeconds;
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }

                var localizer = context.HttpContext.RequestServices.GetRequiredService<IStringLocalizerService>();
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    status = StatusCodes.Status429TooManyRequests,
                    title = localizer.Get(ResourceKeys.TooManyRequests),
                    retryAfterSeconds
                }, token);
            };

            options.AddPolicy(PolicyName, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"{httpContext.Connection.RemoteIpAddress}|{httpContext.Request.Path}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromSeconds(30),
                        QueueLimit = 0
                    }));
        });

        return services;
    }
}
