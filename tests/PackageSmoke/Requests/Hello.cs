using SwiftDispatch.Contracts;
using SwiftDispatch.PackageSmoke.Infrastructure;

namespace SwiftDispatch.PackageSmoke.Requests;

public sealed record Hello(string Name) : IRequest<string>;

public sealed class HelloHandler(TraceLog trace) : IRequestHandler<Hello, string>
{
    public Task<string> Handle(Hello request, CancellationToken cancellationToken)
    {
        trace.Entries.Add("hello");
        return Task.FromResult($"Hello, {request.Name}!");
    }
}
