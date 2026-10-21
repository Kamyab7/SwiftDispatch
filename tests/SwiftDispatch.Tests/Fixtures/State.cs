namespace SwiftDispatch.Tests.Fixtures;

internal sealed class State
{
    public List<string> Entries { get; } = [];
    public int Attempts { get; set; }
    public bool ShortCircuit { get; set; }
    public bool Retry { get; set; }
    public bool Fail { get; set; }
    public bool Caught { get; set; }
    public int HandlersDisposed { get; set; }
    public CancellationToken Token { get; set; }
    public CancellationTokenSource? CancelInOuter { get; set; }
    public CancellationTokenSource? CancelInObserver { get; set; }
    public InvalidOperationException Failure { get; } = new("expected failure");
}
