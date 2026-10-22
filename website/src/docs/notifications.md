# Notifications

Notifications publish an event to multiple handlers. SwiftDispatch executes notification handlers sequentially.

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

See the [notification sample](https://github.com/Kamyab7/SwiftDispatch/tree/master/samples/SwiftDispatch.Sample.Console/Notifications) for multiple handlers.
