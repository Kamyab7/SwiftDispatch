namespace TrueMediator.EntityFrameworkCore;

/// <summary>Selects a response or void request for a generated EF Core transaction boundary.</summary>
/// <remarks>Queries, streams, and notifications cannot be transactional. Attributes are not inherited.</remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class TransactionalAttribute : Attribute
{
    /// <summary>Uses the default context selected with UseEntityFrameworkTransactions.</summary>
    public TransactionalAttribute() { }

    /// <summary>Uses the specified DbContext type instead of the configured default.</summary>
    public TransactionalAttribute(Type contextType) => ContextType = contextType;

    /// <summary>The explicit context type, or null when the configured default is used.</summary>
    public Type? ContextType { get; }
}
