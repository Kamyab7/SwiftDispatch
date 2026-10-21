using System.ComponentModel;

namespace SwiftDispatch;

/// <summary>A statically closed request adapter implemented by the source generator.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IRequestDispatcher<TResponse>
{
    Task<TResponse> Invoke(object request, IServiceProvider services, CancellationToken cancellationToken);
}
