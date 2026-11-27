using TrueMediator.Contracts;

namespace TrueMediator.Sample.Console;

public sealed record GreetingCreated(string Greeting) : INotification;

public sealed class GreetingCreatedHandler : INotificationHandler<GreetingCreated>
{
    public Task Handle(GreetingCreated notification, CancellationToken cancellationToken)
    {
        System.Console.WriteLine($"Notification received: {notification.Greeting}");
        return Task.CompletedTask;
    }
}
