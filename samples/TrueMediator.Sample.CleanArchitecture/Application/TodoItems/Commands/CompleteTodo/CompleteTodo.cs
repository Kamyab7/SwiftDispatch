using TrueMediator.Contracts;

namespace TrueMediator.Sample.CleanArchitecture.Application.TodoItems.Commands.CompleteTodo;

public sealed record CompleteTodo(Guid Id) : IRequest<TodoResponse?>;
