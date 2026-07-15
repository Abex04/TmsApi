using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TmsApi.ExceptionHandlers;

// Single place where ALL exceptions are translated into RFC 7807 ProblemDetails.
// Three production habits here:
// 1. ValidationException → 400 with structured errors dictionary keyed by field
//    (same shape as [ApiController] model state failures — one error contract
//    for the Angular team to handle, regardless of where validation ran)
// 2. Unexpected exceptions → 500 with trace ID so support can grep the logs
// 3. Never leaks exception messages or stack traces to the client
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct)
    {
        var (status, title, detail, errors) = exception switch
        {
            // FluentValidation failure — map to 400 with field-level errors
            ValidationException ve => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                "One or more fields are invalid. See errors for details.",
                (IDictionary<string, string[]>?)ve.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray())),

            // Anything else — 500, log it, never expose internals to the client
            _ => (
                StatusCodes.Status500InternalServerError,
                "Server error",
                $"An unexpected error occurred. Trace ID: {httpContext.TraceIdentifier}",
                null)
        };

        // Only log unexpected errors — validation failures are expected, not bugs
        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception,
                "Unhandled exception (trace={TraceId})", httpContext.TraceIdentifier);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        // Attach field-level errors for validation failures
        if (errors is not null)
            problem.Extensions["errors"] = errors;

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, ct);

        return true; // true = exception was handled, stop propagation
    }
}
