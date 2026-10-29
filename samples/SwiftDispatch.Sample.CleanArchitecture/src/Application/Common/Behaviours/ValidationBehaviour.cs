using FluentValidation;
using FluentValidation.Results;
using SwiftDispatch.Contracts;
using SwiftDispatch.Sample.CleanArchitecture.Application.Common.Exceptions;

namespace SwiftDispatch.Sample.CleanArchitecture.Application.Common.Behaviours;

public sealed class ValidationBehaviour<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var failures = new List<ValidationFailure>();

        // Run sequentially so validators can safely share scoped dependencies.
        foreach (IValidator<TRequest> validator in validators)
        {
            var context = new ValidationContext<TRequest>(request);
            ValidationResult result = await validator.ValidateAsync(context, cancellationToken);
            failures.AddRange(result.Errors);
        }

        if (failures.Count > 0)
        {
            throw new RequestValidationException(failures);
        }

        return await next();
    }
}
