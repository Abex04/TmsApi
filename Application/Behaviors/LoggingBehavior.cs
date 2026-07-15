using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TmsApi.Application.Behaviors;

// Runs around every MediatR request — logs start, end, and duration.
// Uses Activity.Current?.TraceId for the correlation ID so the same ID
// appears in both the log and the response, making a request greppable
// across the entire log stream.
// MUST be registered BEFORE ValidationBehavior so it wraps validation
// failures inside its log scope — reverse order = silent validation failures.
public class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;

        // Use the current Activity trace ID if available (set by ASP.NET Core
        // distributed tracing), otherwise generate a fallback correlation ID.
        var correlationId = Activity.Current?.TraceId.ToString()
            ?? Guid.NewGuid().ToString("N");

        var stopwatch = Stopwatch.StartNew();

        // BeginScope attaches RequestName and CorrelationId to every log line
        // inside this block — structured logging, not string concatenation.
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestName"] = requestName,
            ["CorrelationId"] = correlationId
        });

        logger.LogInformation("Handling {RequestName} (cid={CorrelationId})",
            requestName, correlationId);

        try
        {
            var response = await next();
            stopwatch.Stop();

            logger.LogInformation(
                "Handled {RequestName} in {ElapsedMs}ms (cid={CorrelationId})",
                requestName, stopwatch.ElapsedMilliseconds, correlationId);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Log the failure with the same correlation ID — grep this ID
            // to see the full request lifecycle in one search.
            logger.LogError(ex,
                "Failed {RequestName} after {ElapsedMs}ms (cid={CorrelationId})",
                requestName, stopwatch.ElapsedMilliseconds, correlationId);

            throw; // Re-throw so IExceptionHandler can translate it to ProblemDetails
        }
    }
}
