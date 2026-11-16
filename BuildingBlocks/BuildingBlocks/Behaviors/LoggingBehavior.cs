using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace BuildingBlocks.Behaviors;

/// <summary>
/// Logs the start and end of every MediatR request and warns when one takes more than 3 seconds.
/// The request data is logged with secrets masked (password, OTP, token, ...) and long values cut,
/// so passwords and big payloads (e.g. a logo data URL) never reach the log files.
/// </summary>
public partial class LoggingBehavior<TRequest, TResponse>
    (ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
    where TResponse : notnull
{
    private static readonly TimeSpan SlowRequestThreshold = TimeSpan.FromSeconds(3);

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        logger.LogInformation("[START] Handle request={Request} - Response={Response} - RequestData={RequestData}",
            typeof(TRequest).Name, typeof(TResponse).Name, SafeLogData.Describe(request));

        var timer = Stopwatch.StartNew();

        var response = await next(cancellationToken);

        timer.Stop();
        if (timer.Elapsed > SlowRequestThreshold)
            logger.LogWarning("[PERFORMANCE] The request {Request} took {TimeTaken} seconds.",
                typeof(TRequest).Name, timer.Elapsed.TotalSeconds);

        logger.LogInformation("[END] Handled {Request} with {Response}", typeof(TRequest).Name, typeof(TResponse).Name);
        return response;
    }
}

/// <summary>Turns a request into log-safe JSON: secrets are masked and long strings are shortened.</summary>
public static partial class SafeLogData
{
    private const int MaxStringLength = 120;
    private const string Masked = "***";

    [GeneratedRegex("password|otp|token|secret|stamp|key", RegexOptions.IgnoreCase)]
    private static partial Regex SensitiveName();

    public static string Describe(object request)
    {
        try
        {
            var node = JsonSerializer.SerializeToNode(request, request.GetType());
            return Clean(node)?.ToJsonString() ?? "{}";
        }
        catch
        {
            return $"({request.GetType().Name})"; // never let logging break the request
        }
    }

    private static JsonNode? Clean(JsonNode? node, string? name = null)
    {
        if (name is not null && SensitiveName().IsMatch(name))
            return JsonValue.Create(Masked);

        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(p => p.Key).ToList())
                    obj[key] = Clean(obj[key]?.DeepClone(), key);
                return obj;
            case JsonArray array:
                for (var i = 0; i < array.Count; i++)
                    array[i] = Clean(array[i]?.DeepClone());
                return array;
            case JsonValue value when value.TryGetValue(out string? text) && text.Length > MaxStringLength:
                return JsonValue.Create($"{text[..MaxStringLength]}… ({text.Length} chars)");
            default:
                return node;
        }
    }
}
