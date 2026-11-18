namespace SwiftDispatch.Diagnostics;

/// <summary>
/// An immutable configured flow. Behaviors may short-circuit or retry; runtime service overrides
/// and nested dispatches can change actual execution. Source locations are not runtime metadata.
/// </summary>
public sealed class DispatchDescriptor
{
    public Type MessageType { get; }
    /// <summary>Null for void requests and notifications.</summary>
    public Type? ResponseType { get; }
    public DispatchKind Kind { get; }
    /// <summary>Applicable closed behaviors, ordered outermost first.</summary>
    public IReadOnlyList<Type> BehaviorTypes { get; }
    /// <summary>Handlers in generated registration order, not necessarily execution order.</summary>
    public IReadOnlyList<Type> HandlerTypes { get; }
    /// <summary>The configured notification strategy, or null for requests and streams.</summary>
    public Type? NotificationStrategyType { get; }

    public DispatchDescriptor(Type messageType, Type? responseType, DispatchKind kind,
        IEnumerable<Type> behaviorTypes, IEnumerable<Type> handlerTypes, Type? notificationStrategyType)
    {
        MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
        ResponseType = responseType;
        Kind = kind;
        BehaviorTypes = Copy(behaviorTypes, nameof(behaviorTypes));
        HandlerTypes = Copy(handlerTypes, nameof(handlerTypes));
        NotificationStrategyType = notificationStrategyType;
    }

    private static IReadOnlyList<Type> Copy(IEnumerable<Type> types, string parameterName)
    {
        if (types is null) throw new ArgumentNullException(parameterName);
        Type[] copy = types.ToArray();
        if (copy.Any(static type => type is null)) throw new ArgumentException("Types must not contain null.", parameterName);
        return Array.AsReadOnly(copy);
    }
}
