namespace SwiftDispatch;

/// <summary>Awaits each handler in order, stopping on failure or cancellation.</summary>
public sealed class SequentialNotificationExecutionStrategy : INotificationExecutionStrategy
{
    public async Task Execute(IEnumerable<Func<CancellationToken, Task>> handlers, CancellationToken cancellationToken)
    {
        if (handlers is null) throw new ArgumentNullException(nameof(handlers));
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var handler in handlers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await handler(cancellationToken).ConfigureAwait(false);
        }
    }
}
