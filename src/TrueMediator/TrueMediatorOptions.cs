namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Build-time directives read by the TrueMediator source generator. Use these methods in one
/// lambda passed directly to AddTrueMediator. The lambda is inspected at build time,
/// not executed at runtime; variables, helper methods and conditional configuration are unsupported.
/// </summary>
public sealed class TrueMediatorOptions
{
    private TrueMediatorOptions() { }

    /// <summary>
    /// Validates concrete request types in the current and explicitly included assemblies for
    /// missing handlers at build time. Enabled by default. Pass a compile-time Boolean constant.
    /// </summary>
    public TrueMediatorOptions ValidateMissingHandlers(bool enabled) => this;

    /// <summary>
    /// Requires at least one handler for each concrete, closed notification in the current and
    /// explicitly included assemblies at build time. Disabled by default. Pass a compile-time Boolean constant.
    /// </summary>
    public TrueMediatorOptions ValidateNotificationHandlers(bool enabled) => this;

    /// <summary>Selects the notification execution strategy. Sequential execution is the default.</summary>
    public TrueMediatorOptions UseNotificationExecutionStrategy<TStrategy>()
        where TStrategy : class, TrueMediator.INotificationExecutionStrategy => this;

    /// <summary>Includes handlers from the assembly containing the marker type.</summary>
    public TrueMediatorOptions AddHandlersFromAssemblyContaining<T>() => this;

    /// <summary>Includes handlers from Assembly.GetExecutingAssembly() or typeof(T).Assembly.</summary>
    public TrueMediatorOptions RegisterServicesFromAssembly(System.Reflection.Assembly assembly) => this;

    /// <summary>Adds an open request behavior, ordered outermost first. Pass a typeof expression.</summary>
    public TrueMediatorOptions AddOpenBehavior(Type behaviorType) => this;

    /// <summary>Adds a request behavior, ordered outermost first. Pass a typeof expression.</summary>
    public TrueMediatorOptions AddPipelineBehavior(Type behaviorType) => this;

    /// <summary>Adds a stream behavior, ordered outermost first. Pass a typeof expression.</summary>
    public TrueMediatorOptions AddStreamPipelineBehavior(Type behaviorType) => this;
}
