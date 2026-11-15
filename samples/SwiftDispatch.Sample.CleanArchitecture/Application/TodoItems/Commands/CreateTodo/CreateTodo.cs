using SwiftDispatch.Contracts;

namespace SwiftDispatch.Sample.CleanArchitecture.Application.TodoItems.Commands.CreateTodo;

public sealed record CreateTodo(string Title) : IRequest<TodoResponse>;
