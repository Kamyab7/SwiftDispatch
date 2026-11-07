using System.Collections.Generic;
using System.Collections.Immutable;

namespace SwiftDispatch.SourceGenerator;

/// <summary>Validated, ordered input to code generation. Contains no Roslyn symbols or mutable analysis state.</summary>
internal sealed class DispatchModel(
    ImmutableArray<RequestDispatchModel> requests,
    ImmutableArray<NotificationDispatchModel> notifications,
    ImmutableArray<ComponentRegistrationModel> components,
    string? notificationStrategyResolution)
{
    public IReadOnlyList<RequestDispatchModel> Requests { get; } = requests;
    public IReadOnlyList<NotificationDispatchModel> Notifications { get; } = notifications;
    public IReadOnlyList<ComponentRegistrationModel> Components { get; } = components;
    public string? NotificationStrategyResolution { get; } = notificationStrategyResolution;
}

internal sealed class RequestDispatchModel(string message, string response, string handlerResolution,
    bool returnsVoid, bool streams, ImmutableArray<string> behaviorResolutions)
{
    public string Message { get; } = message;
    public string Response { get; } = response;
    public string HandlerResolution { get; } = handlerResolution;
    public bool ReturnsVoid { get; } = returnsVoid;
    public bool Streams { get; } = streams;
    public IReadOnlyList<string> BehaviorResolutions { get; } = behaviorResolutions;
}

internal sealed class NotificationDispatchModel(string message, ImmutableArray<string> handlerResolutions)
{
    public string Message { get; } = message;
    public IReadOnlyList<string> HandlerResolutions { get; } = handlerResolutions;
}

internal sealed class ComponentRegistrationModel(string typeName, ImmutableArray<string> constructorArguments)
{
    public string TypeName { get; } = typeName;
    public IReadOnlyList<string> ConstructorArguments { get; } = constructorArguments;
}
