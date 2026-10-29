using SwiftDispatch.Sample.CleanArchitecture.Domain.Entities;

namespace SwiftDispatch.Sample.CleanArchitecture.Application.Common.Interfaces;

public interface ITodoRepository
{
    Task Add(TodoItem item, CancellationToken cancellationToken);
    Task<TodoItem?> Get(Guid id, CancellationToken cancellationToken);
    Task Save(TodoItem item, CancellationToken cancellationToken);
}
