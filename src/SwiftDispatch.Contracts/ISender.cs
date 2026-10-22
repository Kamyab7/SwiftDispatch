namespace SwiftDispatch.Contracts;

public interface ISender
{
    IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default);

    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest;
}
