namespace SwiftDispatch.Contracts;

public delegate IAsyncEnumerable<TResponse> StreamHandlerDelegate<out TResponse>();

public interface IStreamPipelineBehavior<in TRequest, TResponse> where TRequest : notnull
{
    IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
