using SwiftDispatch.Contracts;

namespace SwiftDispatch.Tests.Fixtures.Notifications;

internal sealed class AObserver(State state) : INotificationHandler<Notice>
{
    public async Task Handle(Notice notification, CancellationToken cancellationToken)
    {
        state.Token = cancellationToken;
        state.Entries.Add("a.start");
        await Task.Yield();
        if (state.Fail)
        {
            throw state.Failure;
        }
        state.Entries.Add("a.end");
        state.CancelInObserver?.Cancel();
    }
}
