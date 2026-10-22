using SwiftDispatch.Contracts;
using SwiftDispatch.Sample.Console.Infrastructure;

namespace SwiftDispatch.Sample.Console.Notifications.Handlers;

public sealed class AuditObserver(TraceLog trace) : INotificationHandler<GreetingSaved>
{
    public Task Handle(GreetingSaved notification, CancellationToken cancellationToken)
    {
        trace.Entries.Add("audit");
        return Task.CompletedTask;
    }
}
