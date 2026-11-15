using SwiftDispatch.Contracts;

namespace SwiftDispatch.Sample.CleanArchitecture.Application.TodoItems.Notifications;

public sealed record TodoCompleted(Guid Id, string Title) : INotification;
