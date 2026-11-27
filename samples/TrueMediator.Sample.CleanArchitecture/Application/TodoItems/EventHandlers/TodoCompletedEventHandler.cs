using Microsoft.Extensions.Logging;
using TrueMediator.Contracts;
using TrueMediator.Sample.CleanArchitecture.Application.TodoItems.Notifications;

namespace TrueMediator.Sample.CleanArchitecture.Application.TodoItems.EventHandlers;

public sealed class TodoCompletedEventHandler(ILogger<TodoCompletedEventHandler> logger)
    : INotificationHandler<TodoCompleted>
{
    public Task Handle(TodoCompleted notification, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation("Todo completed: {TodoId} ({Title})", notification.Id, notification.Title);
        return Task.CompletedTask;
    }
}
