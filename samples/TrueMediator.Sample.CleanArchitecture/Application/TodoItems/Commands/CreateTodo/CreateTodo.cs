using TrueMediator.Contracts;

namespace TrueMediator.Sample.CleanArchitecture.Application.TodoItems.Commands.CreateTodo;

public sealed record CreateTodo(string Title) : IRequest<TodoResponse>;
