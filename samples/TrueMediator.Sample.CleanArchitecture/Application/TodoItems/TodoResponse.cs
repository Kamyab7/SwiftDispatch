using TrueMediator.Sample.CleanArchitecture.Domain.Entities;

namespace TrueMediator.Sample.CleanArchitecture.Application.TodoItems;

public sealed record TodoResponse(Guid Id, string Title, bool IsCompleted)
{
    public static TodoResponse From(TodoItem item) => new(item.Id, item.Title, item.IsCompleted);
}

