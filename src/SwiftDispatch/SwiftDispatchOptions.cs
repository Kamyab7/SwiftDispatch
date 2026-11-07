namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Build-time directives read by the SwiftDispatch source generator. Use these methods in one
/// lambda passed directly to AddSwiftDispatch. The lambda is inspected at build time,
/// not executed at runtime; variables, helper methods and conditional configuration are unsupported.
/// </summary>
public sealed class SwiftDispatchOptions
{
    private SwiftDispatchOptions() { }

    /// <summary>Selects the notification execution strategy. Sequential execution is the default.</summary>
    public SwiftDispatchOptions UseNotificationExecutionStrategy<TStrategy>()
        where TStrategy : class, SwiftDispatch.INotificationExecutionStrategy => this;

    /// <summary>Includes handlers from the assembly containing the marker type.</summary>
    public SwiftDispatchOptions AddHandlersFromAssemblyContaining<T>() => this;

    /// <summary>Includes handlers from Assembly.GetExecutingAssembly() or typeof(T).Assembly.</summary>
    public SwiftDispatchOptions RegisterServicesFromAssembly(System.Reflection.Assembly assembly) => this;

    /// <summary>Adds an open request behavior, ordered outermost first. Pass a typeof expression.</summary>
    public SwiftDispatchOptions AddOpenBehavior(Type behaviorType) => this;

    /// <summary>Adds a request behavior, ordered outermost first. Pass a typeof expression.</summary>
    public SwiftDispatchOptions AddPipelineBehavior(Type behaviorType) => this;

    /// <summary>Adds a stream behavior, ordered outermost first. Pass a typeof expression.</summary>
    public SwiftDispatchOptions AddStreamPipelineBehavior(Type behaviorType) => this;
}
