using System.Runtime.CompilerServices;
using TrueMediator.Contracts;

namespace TrueMediator.PackageSmoke.Requests;

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
