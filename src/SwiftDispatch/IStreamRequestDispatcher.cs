using System.ComponentModel;

namespace SwiftDispatch;

/// <summary>A statically closed stream adapter implemented by the source generator.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IStreamRequestDispatcher<TResponse>
{
    IAsyncEnumerable<TResponse> Invoke(object request, IServiceProvider services, CancellationToken cancellationToken);
}
