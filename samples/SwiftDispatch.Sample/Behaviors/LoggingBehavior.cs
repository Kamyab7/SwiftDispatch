using SwiftDispatch.Contracts;
using SwiftDispatch.Sample.Infrastructure;

namespace SwiftDispatch.Sample.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(TraceLog trace) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        trace.Entries.Add("before");
        TResponse response = await next().ConfigureAwait(false);
        trace.Entries.Add("after");
        return response;
    }
}
