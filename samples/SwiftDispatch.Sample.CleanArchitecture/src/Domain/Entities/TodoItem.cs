namespace SwiftDispatch.Sample.CleanArchitecture.Domain.Entities;

public sealed class TodoItem
{
    public TodoItem(Guid id, string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Id = id;
        Title = title.Trim();
    }

    public Guid Id { get; }
    public string Title { get; }
    public bool IsCompleted { get; private set; }

    public void Complete() => IsCompleted = true;
}
