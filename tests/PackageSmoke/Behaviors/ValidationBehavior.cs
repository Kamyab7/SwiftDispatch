using SwiftDispatch.Contracts;
using SwiftDispatch.PackageSmoke.Requests;

namespace SwiftDispatch.PackageSmoke.Behaviors;

public sealed class ValidationBehavior : IPipelineBehavior<Hello, string>
{
    public Task<string> Handle(Hello request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);
        return next();
    }
}
