# Notifications

Notifications publish an event to multiple handlers. SwiftDispatch executes notification handlers sequentially by default.

## Define a notification

```csharp
using SwiftDispatch.Contracts;

public sealed record OrderPlaced(string OrderId) : INotification;
```

## Add a handler

```csharp
public sealed class OrderAuditHandler : INotificationHandler<OrderPlaced>
{
    public Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Order placed: {notification.OrderId}");
        return Task.CompletedTask;
    }
}
```

Register services and configure a generated registration class as described in [getting started](getting-started.md). Add more `INotificationHandler<OrderPlaced>` implementations for other reactions to the event.

## Publish an event

```csharp
await mediator.Publish(new OrderPlaced("order-123"));
```

See the [package smoke test handlers](https://github.com/Kamyab7/SwiftDispatch/tree/master/tests/PackageSmoke/Notifications/Handlers) for multiple handlers.

## Execution strategies

Select parallel execution inside `AddSwiftDispatch`:

```csharp
using SwiftDispatch;

services.AddSwiftDispatch(options => options
    .AddHandlersFromAssemblyContaining<OrderPlaced>()
    .UseNotificationExecutionStrategy<ParallelNotificationExecutionStrategy>());
```

`SequentialNotificationExecutionStrategy` awaits handlers in stable generated order and stops on the first failure or cancellation. `ParallelNotificationExecutionStrategy` starts every handler and awaits them all, including when a handler throws synchronously. Parallel execution overlaps asynchronous work; it does not move handlers onto background threads. Handlers share the publication's service scope, so their dependencies must support concurrent use.

Implement `INotificationExecutionStrategy` and select it with `UseNotificationExecutionStrategy<MyStrategy>()` to provide custom execution behavior. Selected strategies support constructor injection and are resolved from the publication scope. Each callback resolves and invokes its handler when called, using the cancellation token supplied by the strategy.
