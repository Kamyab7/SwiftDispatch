using System.Collections.Concurrent;
using SwiftDispatch.Sample.CleanArchitecture.Application.Common.Interfaces;
using SwiftDispatch.Sample.CleanArchitecture.Domain.Entities;

namespace SwiftDispatch.Sample.CleanArchitecture.Infrastructure.Persistence;

public sealed class InMemoryTodoRepository : ITodoRepository
{
    private readonly ConcurrentDictionary<Guid, TodoItem> _items = new();

    public Task Add(TodoItem item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_items.TryAdd(item.Id, Copy(item)))
        {
            throw new InvalidOperationException("A todo with this ID already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<TodoItem?> Get(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_items.TryGetValue(id, out TodoItem? item) ? Copy(item) : null);
    }

    public Task Save(TodoItem item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _items[item.Id] = Copy(item);
        return Task.CompletedTask;
    }

    // Keep mutable domain objects isolated from the shared store.
    private static TodoItem Copy(TodoItem item)
    {
        var copy = new TodoItem(item.Id, item.Title);
        if (item.IsCompleted)
        {
            copy.Complete();
        }

        return copy;
    }
}
