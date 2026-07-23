using FluentValidation;
using MediatR;

namespace TmsApi.Application.Behaviors;

// Runs before every MediatR handler — validates the request using any
// registered IValidator<TRequest> implementations.
// If validation fails, throws ValidationException which is caught by
// GlobalExceptionHandler and translated into a 400 Bad Request.
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

        // Run all validators and collect every failure
        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(result => result.Errors)
            .Where(f => f is not null)
            .ToList();

        // If any failures exist, throw — GlobalExceptionHandler catches this
        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}