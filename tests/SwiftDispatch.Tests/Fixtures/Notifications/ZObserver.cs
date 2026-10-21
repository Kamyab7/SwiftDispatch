using SwiftDispatch.Contracts;

namespace SwiftDispatch.Tests.Fixtures.Notifications;

internal sealed class ZObserver(State state) : INotificationHandler<Notice>
{
    public Task Handle(Notice notification, CancellationToken cancellationToken)
    {
        state.Entries.Add("z");
        return Task.CompletedTask;
    }
}
