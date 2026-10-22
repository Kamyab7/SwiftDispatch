#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif
using System.ComponentModel;

namespace SwiftDispatch;

/// <summary>Infrastructure used by generated code. Contains no service instances.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DispatchTable
{
    internal IReadOnlyDictionary<Type, object> Requests { get; }
    internal IReadOnlyDictionary<Type, NotificationDispatcher> Notifications { get; }

    public DispatchTable(IReadOnlyDictionary<Type, object> requests, IReadOnlyDictionary<Type, NotificationDispatcher> notifications)
    {
#if NET8_0_OR_GREATER
        Requests = requests.ToFrozenDictionary();
        Notifications = notifications.ToFrozenDictionary();
#else
        Requests = requests.ToDictionary(static pair => pair.Key, static pair => pair.Value);
        Notifications = notifications.ToDictionary(static pair => pair.Key, static pair => pair.Value);
#endif
    }
}

