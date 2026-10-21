using System.ComponentModel;

namespace SwiftDispatch;

/// <summary>A statically closed notification adapter implemented by the source generator.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class NotificationDispatcher
{
    public abstract Task Invoke(object notification, IServiceProvider services, CancellationToken cancellationToken);
}
