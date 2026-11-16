namespace BuildingBlocks.Exceptions.Handler;

/// <summary>Turns exceptions into RFC 7807 ProblemDetails responses with the right status code.</summary>
public class CustomExceptionHandler
    (ILogger<CustomExceptionHandler> logger)
    : IExceptionHandler
{
    /// <summary>Non-standard "client closed request" status (the response is never seen by the client anyway).</summary>
    private const int Status499ClientClosedRequest = 499;

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        // The browser cancelled the request (page reload, navigation, closed tab): not a server error.
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Request {Method} {Path} was cancelled by the client",
                context.Request.Method, context.Request.Path);
            context.Response.StatusCode = Status499ClientClosedRequest;
            return true;
        }

        logger.LogError(exception,
            "Error Message: {ExceptionMessage}, Time of occurrence {Time}",
            exception.Message, DateTime.UtcNow);

        var statusCode = exception switch
        {
            ValidationException => StatusCodes.Status400BadRequest,
            BadRequestException => StatusCodes.Status400BadRequest,
            NotFoundException => StatusCodes.Status404NotFound,
            BadHttpRequestException e => e.StatusCode, // malformed JSON, missing route/query values
            InternalServerException => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Title = exception.GetType().Name,
            Detail = exception.Message,
            Status = statusCode,
            Instance = context.Request.Path
        };

        problemDetails.Extensions.Add("traceId", context.TraceIdentifier);

        // Machine-readable code for clients, e.g. "USER_ALREADY_LOGGED_IN"
        var errorCode = exception switch
        {
            BadRequestException e => e.Details,
            InternalServerException e => e.Details,
            _ => null
        };
        if (errorCode is not null)
            problemDetails.Extensions.Add("errorCode", errorCode);

        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions.Add("ValidationErrors",
                validationException.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));
        }

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(problemDetails, cancellationToken: cancellationToken);
        return true;
    }
}
