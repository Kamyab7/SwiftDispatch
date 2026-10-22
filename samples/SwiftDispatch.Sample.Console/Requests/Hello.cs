using SwiftDispatch.Contracts;
using SwiftDispatch.Sample.Console.Infrastructure;

namespace SwiftDispatch.Sample.Console.Requests;

public sealed record Hello(string Name) : IRequest<string>;

public sealed class HelloHandler(TraceLog trace) : IRequestHandler<Hello, string>
{
    public Task<string> Handle(Hello request, CancellationToken cancellationToken)
    {
        trace.Entries.Add("hello");
        return Task.FromResult($"Hello, {request.Name}!");
    }
}
