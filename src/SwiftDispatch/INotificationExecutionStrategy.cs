namespace SwiftDispatch;

/// <summary>Controls how the handlers for one notification are executed.</summary>
public interface INotificationExecutionStrategy
{
    /// <summary>Executes handler callbacks in their stable generated order.</summary>
    Task Execute(IEnumerable<Func<CancellationToken, Task>> handlers, CancellationToken cancellationToken);
}
