namespace SwiftDispatch.Contracts;

/// <summary>Generates registration and dispatch in a top-level, non-generic static partial class.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateSwiftDispatchAttribute : Attribute
{
    /// <summary>Marker types in referenced assemblies whose public handlers should be included.</summary>
    public Type[] HandlerAssemblyMarkers { get; set; } = [];

    /// <summary>Stream behaviors, ordered outermost first.</summary>
    public Type[] StreamPipelineBehaviors { get; set; } = [];

    /// <summary>Closed behaviors or open two-parameter behaviors, ordered outermost first.</summary>
    public Type[] PipelineBehaviors { get; set; } = [];
}
