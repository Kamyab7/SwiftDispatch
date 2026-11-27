using TrueMediator.Contracts;
using TrueMediator.Sample.CleanArchitecture.Application.Common.Interfaces;
using TrueMediator.Sample.CleanArchitecture.Domain.Entities;

namespace TrueMediator.Sample.CleanArchitecture.Application.TodoItems.Commands.CreateTodo;

public sealed class CreateTodoHandler(ITodoRepository repository) : IRequestHandler<CreateTodo, TodoResponse>
{
    public async Task<TodoResponse> Handle(CreateTodo request, CancellationToken cancellationToken)
    {
        var item = new TodoItem(Guid.NewGuid(), request.Title);
        await repository.Add(item, cancellationToken);
        return TodoResponse.From(item);
    }
}
