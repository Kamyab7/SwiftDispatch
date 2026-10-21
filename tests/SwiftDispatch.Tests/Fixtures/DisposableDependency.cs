namespace SwiftDispatch.Tests.Fixtures;

internal sealed class DisposableDependency : IDisposable
{
    public bool Disposed { get; private set; }
    public int HandlersCreated { get; set; }
    public void Dispose() => Disposed = true;
}
