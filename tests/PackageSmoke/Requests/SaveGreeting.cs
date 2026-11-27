using TrueMediator.Contracts;
using TrueMediator.PackageSmoke.Infrastructure;

namespace TrueMediator.PackageSmoke.Requests;

public sealed record SaveGreeting(string Greeting) : ICommand;

public sealed class SaveGreetingHandler(TraceLog trace) : IRequestHandler<SaveGreeting>
{
    public Task Handle(SaveGreeting request, CancellationToken cancellationToken)
    {
        trace.Entries.Add("saved: " + request.Greeting);
        return Task.CompletedTask;
    }
}
