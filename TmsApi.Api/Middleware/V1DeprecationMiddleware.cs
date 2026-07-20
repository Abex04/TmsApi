namespace TmsApi.Api.Middleware;

// Stamps three deprecation headers on every V1 response:
// - Deprecation: true (IETF draft)
// - Sunset: <date> (RFC 8594 — the date V1 will stop responding)
// - Link: <V2 URL>; rel="successor-version" (RFC 5988)
public class V1DeprecationMiddleware(RequestDelegate next)
{
    private static readonly DateTimeOffset SunsetDate =
        new(2026, 12, 31, 0, 0, 0, TimeSpan.Zero);

    public async Task InvokeAsync(HttpContext context)
    {
        // OnStarting fires just before response headers are flushed —
        // the last moment we can still add headers.
        context.Response.OnStarting(() =>
        {
            if (context.Request.Path.StartsWithSegments("/api/v1"))
            {
                context.Response.Headers["Deprecation"] = "true";
                context.Response.Headers["Sunset"] = SunsetDate.ToString("R");
                context.Response.Headers["Link"] =
                    $"<{context.Request.Scheme}://{context.Request.Host}" +
                    $"/api/v2{context.Request.Path.Value?[7..]}>; rel=\"successor-version\"";
            }
            return Task.CompletedTask;
        });

        await next(context);
    }
}
