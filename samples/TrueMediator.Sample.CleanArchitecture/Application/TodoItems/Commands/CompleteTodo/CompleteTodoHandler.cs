using TrueMediator.Contracts;
using TrueMediator.Sample.CleanArchitecture.Application.Common.Interfaces;
using TrueMediator.Sample.CleanArchitecture.Application.TodoItems.Notifications;
using TrueMediator.Sample.CleanArchitecture.Domain.Entities;

namespace TrueMediator.Sample.CleanArchitecture.Application.TodoItems.Commands.CompleteTodo;

public sealed class CompleteTodoHandler(ITodoRepository repository, IPublisher publisher) : IRequestHandler<CompleteTodo, TodoResponse?>
{
    public async Task<TodoResponse?> Handle(CompleteTodo request, CancellationToken cancellationToken)
    {
        TodoItem? item = await repository.Get(request.Id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        if (item.IsCompleted)
        {
            return TodoResponse.From(item);
        }

        item.Complete();
        await repository.Save(item, cancellationToken);
        await publisher.Publish(new TodoCompleted(item.Id, item.Title), cancellationToken);
        return TodoResponse.From(item);
    }
}
