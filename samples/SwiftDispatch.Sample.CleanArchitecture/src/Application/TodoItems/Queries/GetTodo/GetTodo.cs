using SwiftDispatch.Contracts;

namespace SwiftDispatch.Sample.CleanArchitecture.Application.TodoItems.Queries.GetTodo;

public sealed record GetTodo(Guid Id) : IRequest<TodoResponse?>;
