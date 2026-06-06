using System.Diagnostics;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate a short unique ID for this request
        var correlationId = Guid.NewGuid().ToString("N")[..8];

        // Add the correlation ID to the response headers BEFORE next()
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        // Log the incoming request
        _logger.LogInformation(
            "Request {Method} {Path} [{CorrelationId}]",
            context.Request.Method,
            context.Request.Path,
            correlationId);

        // Start timing
        var stopwatch = Stopwatch.StartNew();

        // Pass control to the next middleware
        await _next(context);

        // Stop timing
        stopwatch.Stop();

        // Log the completed response
        _logger.LogInformation(
            "Response {StatusCode} {ElapsedMs}ms [{CorrelationId}]",
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds,
            correlationId);
    }
}