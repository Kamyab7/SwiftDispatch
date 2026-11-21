using FluentValidation;
using FluentValidation.Results;
using SwiftDispatch.Contracts;

namespace SwiftDispatch.FluentValidation;

/// <summary>Validates requests before continuing the request pipeline.</summary>
/// <remarks>
/// Validators execute sequentially in DI registration order so they can share scoped dependencies.
/// Register this behavior explicitly with AddOpenBehavior. Streaming requests and notifications
/// are not validated by this behavior.
/// </remarks>
public sealed class FluentValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>Runs all registered validators asynchronously and continues only when validation succeeds.</summary>
    /// <exception cref="ValidationException">One or more validators returned validation failures.</exception>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<ValidationFailure>? failures = null;

        foreach (IValidator<TRequest> validator in validators)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var context = new ValidationContext<TRequest>(request);
            ValidationResult result = await validator.ValidateAsync(context, cancellationToken).ConfigureAwait(false);
            if (result.Errors.Count > 0)
            {
                failures ??= new List<ValidationFailure>();
                failures.AddRange(result.Errors);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (failures is { Count: > 0 })
        {
            throw new ValidationException(failures);
        }

        return await next().ConfigureAwait(false);
    }
}
