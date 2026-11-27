using TrueMediator.Contracts;

namespace TrueMediator.Sample.CleanArchitecture.Application.TodoItems.Queries.GetTodo;

public sealed record GetTodo(Guid Id) : IRequest<TodoResponse?>;
