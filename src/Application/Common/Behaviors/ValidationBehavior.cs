using FluentValidation;
using MediatR;

namespace clean.Application.Common.Behaviors;

public interface INormalizableRequest { void Normalize(); }

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is INormalizableRequest normalized) normalized.Normalize();
        var failures = new List<FluentValidation.Results.ValidationFailure>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(new ValidationContext<TRequest>(request), cancellationToken);
            failures.AddRange(result.Errors);
        }
        if (failures.Count > 0) throw new ValidationException(failures);
        return await next();
    }
}
