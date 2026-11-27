using TrueMediator.Contracts;
using TrueMediator.Sample.CleanArchitecture.Application.Common.Interfaces;
using TrueMediator.Sample.CleanArchitecture.Domain.Entities;

namespace TrueMediator.Sample.CleanArchitecture.Application.TodoItems.Queries.GetTodo;

public sealed class GetTodoHandler(ITodoRepository repository) : IRequestHandler<GetTodo, TodoResponse?>
{
    public async Task<TodoResponse?> Handle(GetTodo request, CancellationToken cancellationToken)
    {
        TodoItem? item = await repository.Get(request.Id, cancellationToken);
        return item is null ? null : TodoResponse.From(item);
    }
}
