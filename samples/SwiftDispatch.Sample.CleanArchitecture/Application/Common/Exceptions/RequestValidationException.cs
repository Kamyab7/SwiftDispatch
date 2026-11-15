using FluentValidation.Results;

namespace SwiftDispatch.Sample.CleanArchitecture.Application.Common.Exceptions;

public sealed class RequestValidationException(IEnumerable<ValidationFailure> failures)
    : Exception("One or more validation failures have occurred.")
{
    public IDictionary<string, string[]> Errors { get; } = failures
        .GroupBy(failure => failure.PropertyName)
        .ToDictionary(
            group => group.Key,
            group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());
}
