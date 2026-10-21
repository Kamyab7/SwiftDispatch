using System.Collections.Frozen;
using System.ComponentModel;

namespace SwiftDispatch;

/// <summary>Infrastructure used by generated code. Contains no service instances.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DispatchTable
{
    internal FrozenDictionary<Type, object> Requests { get; }
    internal FrozenDictionary<Type, NotificationDispatcher> Notifications { get; }

    public DispatchTable(IReadOnlyDictionary<Type, object> requests, IReadOnlyDictionary<Type, NotificationDispatcher> notifications)
    {
        Requests = requests.ToFrozenDictionary();
        Notifications = notifications.ToFrozenDictionary();
    }
}

