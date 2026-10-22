using SwiftDispatch.Contracts;

namespace SwiftDispatch.Sample.WebApi.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling {RequestType}", typeof(TRequest).Name);
        try
        {
            TResponse response = await next().ConfigureAwait(false);
            logger.LogInformation("Handled {RequestType}", typeof(TRequest).Name);
            return response;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to handle {RequestType}", typeof(TRequest).Name);
            throw;
        }
    }
}
