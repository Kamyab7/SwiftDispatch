using SwiftDispatch.Contracts;

namespace SwiftDispatch.Tests.Fixtures.Requests;

internal readonly record struct StructQuery(int Value) : IRequest<int>;

internal sealed class StructHandler : IRequestHandler<StructQuery, int>
{
    public Task<int> Handle(StructQuery request, CancellationToken cancellationToken) => Task.FromResult(request.Value);
}
