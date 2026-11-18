namespace SwiftDispatch.Diagnostics;

/// <summary>Build-time configured dispatch flows; does not execute or resolve application components.</summary>
public interface IDispatchCatalog
{
    /// <summary>All generated flows, sorted by message name and dispatch kind.</summary>
    IReadOnlyList<DispatchDescriptor> Entries { get; }

    /// <summary>Returns all flows for the exact message type, or an empty collection.</summary>
    /// <exception cref="ArgumentNullException">The message type is null.</exception>
    IReadOnlyList<DispatchDescriptor> Find(Type messageType);
}
