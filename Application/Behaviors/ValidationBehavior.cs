using FluentValidation;
using MediatR;

namespace TmsApi.Application.Behaviors;

// Runs before every MediatR handler — validates the request using any
// registered IValidator<TRequest> implementations.
// If validation fails, throws ValidationException which is caught by
// GlobalExceptionHandler and translated into a 400 Bad Request with
// ProblemDetails — one central place for all validation error responses.
// Why throw here instead of returning Result.Failure?
// Validation failures are input errors, not business outcomes. Every
// controller would have to translate them back to 400 — duplicated logic.
// Throwing is the cheap, central path that IExceptionHandler handles once.
public class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        // If no validators are registered for this request, skip validation
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        // Run all validators and collect every failure across all of them
        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(result => result.Errors)
            .Where(f => f is not null)
            .ToList();

        // If any failures exist, throw — GlobalExceptionHandler catches this
        // and returns 400 Bad Request with a structured errors dictionary
        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}
