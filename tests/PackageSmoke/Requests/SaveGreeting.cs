using SwiftDispatch.Contracts;
using SwiftDispatch.PackageSmoke.Infrastructure;

namespace SwiftDispatch.PackageSmoke.Requests;

public sealed record SaveGreeting(string Greeting) : IRequest;

public sealed class SaveGreetingHandler(TraceLog trace) : IRequestHandler<SaveGreeting>
{
    public Task Handle(SaveGreeting request, CancellationToken cancellationToken)
    {
        trace.Entries.Add("saved: " + request.Greeting);
        return Task.CompletedTask;
    }
}
