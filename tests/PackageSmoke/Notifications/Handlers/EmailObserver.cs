using TrueMediator.Contracts;
using TrueMediator.PackageSmoke.Infrastructure;

namespace TrueMediator.PackageSmoke.Notifications.Handlers;

public sealed class EmailObserver(TraceLog trace) : INotificationHandler<GreetingSaved>
{
    public Task Handle(GreetingSaved notification, CancellationToken cancellationToken)
    {
        trace.Entries.Add("email");
        return Task.CompletedTask;
    }
}
