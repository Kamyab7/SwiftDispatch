using TrueMediator.Contracts;
using TrueMediator.PackageSmoke.Infrastructure;

namespace TrueMediator.PackageSmoke.Requests;

public sealed record Hello(string Name) : IQuery<string>;

public sealed class HelloHandler(TraceLog trace) : IRequestHandler<Hello, string>
{
    public Task<string> Handle(Hello request, CancellationToken cancellationToken)
    {
        trace.Entries.Add("hello");
        return Task.FromResult($"Hello, {request.Name}!");
    }
}
