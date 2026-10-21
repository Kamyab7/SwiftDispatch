using SwiftDispatch.Contracts;

namespace SwiftDispatch;

public sealed class Mediator(IServiceProvider services, DispatchTable dispatchTable) : IMediator
{
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return GetDispatcher<TResponse>(request).Invoke(request, services, cancellationToken);
    }

    public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        await GetDispatcher<Unit>(request).Invoke(request, services, cancellationToken).ConfigureAwait(false);
    }

    public Task Publish(INotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        cancellationToken.ThrowIfCancellationRequested();
        return dispatchTable.Notifications.TryGetValue(notification.GetType(), out NotificationDispatcher? dispatcher)
            ? dispatcher.Invoke(notification, services, cancellationToken)
            : Task.CompletedTask;
    }

    private IRequestDispatcher<TResponse> GetDispatcher<TResponse>(object request)
    {
        if (dispatchTable.Requests.TryGetValue(request.GetType(), out object? dispatcher)
            && dispatcher is IRequestDispatcher<TResponse> typedDispatcher)
        {
            return typedDispatcher;
        }

        throw new InvalidOperationException($"No generated request handler for '{request.GetType()}'. Include its assembly in GenerateSwiftDispatch.HandlerAssemblyMarkers and call the generated AddSwiftDispatch method.");
    }
}
