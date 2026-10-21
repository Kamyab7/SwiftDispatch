using SwiftDispatch.Contracts;

namespace SwiftDispatch.Tests.Fixtures.Requests;

internal class Query : IRequest<int>;

internal sealed class DerivedQuery : Query;

internal sealed class QueryHandler : IRequestHandler<Query, int>, IDisposable
{
    private readonly State _state;

    public QueryHandler(State state, DisposableDependency dependency)
    {
        _state = state;
        dependency.HandlersCreated++;
    }

    public Task<int> Handle(Query request, CancellationToken cancellationToken)
    {
        _state.Entries.Add("handler");
        _state.Token = cancellationToken;
        _state.Attempts++;
        if (_state.Fail || _state.Retry && _state.Attempts == 1)
        {
            throw _state.Failure;
        }
        return Task.FromResult(42);
    }

    public void Dispose() => _state.HandlersDisposed++;
}
