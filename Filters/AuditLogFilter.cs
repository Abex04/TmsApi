using Microsoft.AspNetCore.Mvc.Filters;

namespace TmsApi.Filters;

// A global action filter — cross-cutting request/response logging that applies
// to every controller action automatically, without any controller needing
// to know this filter exists. This should NEVER contain business logic
// (e.g. "is this course full?") — that decision belongs in the service layer.
public class AuditLogFilter(ILogger<AuditLogFilter> logger) : IActionFilter
{
    // Runs BEFORE the controller action executes.
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var route = context.HttpContext.Request.Path;
        var method = context.HttpContext.Request.Method;
        logger.LogInformation("TMS API call: {Method} {Route}", method, route);
    }

    // Runs AFTER the controller action has finished executing.
    public void OnActionExecuted(ActionExecutedContext context)
    {
        var status = context.HttpContext.Response.StatusCode;
        logger.LogInformation("TMS API response: {StatusCode}", status);
    }
}