using SwiftDispatch.Contracts;
using SwiftDispatch.PackageSmoke.Infrastructure;

namespace SwiftDispatch.PackageSmoke.Notifications.Handlers;

public sealed class EmailObserver(TraceLog trace) : INotificationHandler<GreetingSaved>
{
    public Task Handle(GreetingSaved notification, CancellationToken cancellationToken)
    {
        trace.Entries.Add("email");
        return Task.CompletedTask;
    }
}
