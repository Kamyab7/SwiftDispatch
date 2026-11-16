namespace SwiftDispatch.Benchmarks;

public sealed record Request(bool Yield) : MediatR.IRequest<int>, global::Mediator.IRequest<int>, SwiftDispatch.Contracts.IRequest<int>;
public sealed record Notification : MediatR.INotification, global::Mediator.INotification, SwiftDispatch.Contracts.INotification;

public sealed class RequestHandler : MediatR.IRequestHandler<Request, int>,
    global::Mediator.IRequestHandler<Request, int>, SwiftDispatch.Contracts.IRequestHandler<Request, int>
{
    public Task<int> Handle(Request request, CancellationToken cancellationToken)
        => request.Yield ? YieldTask() : Task.FromResult(42);

    ValueTask<int> global::Mediator.IRequestHandler<Request, int>.Handle(Request request, CancellationToken cancellationToken)
        => request.Yield ? YieldValueTask() : ValueTask.FromResult(42);

    private static async Task<int> YieldTask() { await Task.Yield(); return 42; }
    private static async ValueTask<int> YieldValueTask() { await Task.Yield(); return 42; }
}

public sealed class NotificationCounter
{
    public int Count;
}

public sealed class NotificationHandler(NotificationCounter counter) :
    MediatR.INotificationHandler<Notification>, global::Mediator.INotificationHandler<Notification>,
    SwiftDispatch.Contracts.INotificationHandler<Notification>
{
    public Task Handle(Notification notification, CancellationToken cancellationToken)
    {
        counter.Count++;
        return Task.CompletedTask;
    }

    ValueTask global::Mediator.INotificationHandler<Notification>.Handle(Notification notification, CancellationToken cancellationToken)
    {
        counter.Count++;
        return ValueTask.CompletedTask;
    }
}
