using SwiftDispatch.Contracts;
using SwiftDispatch.Sample.CleanArchitecture.Application.Common.Interfaces;
using SwiftDispatch.Sample.CleanArchitecture.Domain.Entities;

namespace SwiftDispatch.Sample.CleanArchitecture.Application.TodoItems.Commands.CompleteTodo;

public sealed class CompleteTodoHandler(ITodoRepository repository) : IRequestHandler<CompleteTodo, TodoResponse?>
{
    public async Task<TodoResponse?> Handle(CompleteTodo request, CancellationToken cancellationToken)
    {
        TodoItem? item = await repository.Get(request.Id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        item.Complete();
        await repository.Save(item, cancellationToken);
        return TodoResponse.From(item);
    }
}
