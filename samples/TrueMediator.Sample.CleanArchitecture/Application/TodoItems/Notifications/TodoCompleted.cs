using TrueMediator.Contracts;

namespace TrueMediator.Sample.CleanArchitecture.Application.TodoItems.Notifications;

public sealed record TodoCompleted(Guid Id, string Title) : INotification;
