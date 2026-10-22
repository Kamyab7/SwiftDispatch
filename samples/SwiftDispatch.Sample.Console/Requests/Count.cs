using System.Runtime.CompilerServices;
using SwiftDispatch.Contracts;

namespace SwiftDispatch.Sample.Console.Requests;

public sealed record Count(int Limit) : IStreamRequest<int>;

public sealed class CountHandler : IStreamRequestHandler<Count, int>
{
    public async IAsyncEnumerable<int> Handle(Count request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int value = 1; value <= request.Limit; value++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return value;
        }
    }
}
