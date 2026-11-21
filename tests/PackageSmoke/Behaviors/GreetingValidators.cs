using FluentValidation;
using SwiftDispatch.PackageSmoke.Requests;

namespace SwiftDispatch.PackageSmoke.Behaviors;

public sealed class HelloValidator : AbstractValidator<Hello>
{
    public HelloValidator()
    {
        RuleFor(request => request.Name).MustAsync(async (name, token) =>
        {
            await Task.Yield();
            token.ThrowIfCancellationRequested();
            return !string.IsNullOrWhiteSpace(name);
        }).WithErrorCode("NameRequired");
    }
}

public sealed class SaveGreetingValidator : AbstractValidator<SaveGreeting>
{
    public SaveGreetingValidator() => RuleFor(request => request.Greeting).NotEmpty();
}
