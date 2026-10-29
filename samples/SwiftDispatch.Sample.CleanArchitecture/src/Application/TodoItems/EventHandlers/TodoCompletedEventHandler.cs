using Microsoft.Extensions.Logging;
using SwiftDispatch.Contracts;
using SwiftDispatch.Sample.CleanArchitecture.Application.TodoItems.Notifications;

namespace SwiftDispatch.Sample.CleanArchitecture.Application.TodoItems.EventHandlers;

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
