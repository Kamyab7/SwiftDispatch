using SwiftDispatch.Contracts;

namespace SwiftDispatch.Tests.Fixtures.Behaviors;

internal sealed class Inner<TRequest, TResponse>(State state) : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        state.Entries.Add("inner.before");
        TResponse response = await next();
        state.Entries.Add("inner.after");
        return response;
    }
}
