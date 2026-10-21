using SwiftDispatch.Contracts;

namespace SwiftDispatch.Tests.Fixtures.Behaviors;

internal sealed class Outer<TRequest, TResponse>(State state) : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (state.ShortCircuit)
        {
            return (TResponse)(object)99;
        }
        state.Entries.Add("outer.before");
        state.CancelInOuter?.Cancel();
        TResponse response;
        try
        {
            response = await next();
        }
        catch (InvalidOperationException)
        {
            state.Caught = true;
            if (!state.Retry)
            {
                throw;
            }
            response = await next();
        }
        state.Entries.Add("outer.after");
        return response;
    }
}
