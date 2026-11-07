namespace SwiftDispatch;

/// <summary>Starts all handlers and awaits their completion together.</summary>
/// <remarks>Handlers share the publication's service scope; their dependencies must support concurrent use.</remarks>
public sealed class ParallelNotificationExecutionStrategy : INotificationExecutionStrategy
{
    public Task Execute(IEnumerable<Func<CancellationToken, Task>> handlers, CancellationToken cancellationToken)
    {
        if (handlers is null) throw new ArgumentNullException(nameof(handlers));
        cancellationToken.ThrowIfCancellationRequested();
        return Task.WhenAll(handlers.Select(handler => Invoke(handler, cancellationToken)));
    }

    private static async Task Invoke(Func<CancellationToken, Task> handler, CancellationToken cancellationToken)
    {
        await handler(cancellationToken).ConfigureAwait(false);
    }
}
