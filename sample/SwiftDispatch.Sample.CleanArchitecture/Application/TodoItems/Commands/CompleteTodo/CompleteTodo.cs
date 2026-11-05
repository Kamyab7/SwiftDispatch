using SwiftDispatch.Contracts;

namespace SwiftDispatch.Sample.CleanArchitecture.Application.TodoItems.Commands.CompleteTodo;

public sealed record CompleteTodo(Guid Id) : IRequest<TodoResponse?>;
