using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SwiftDispatch.Contracts;
using SwiftDispatch.Sample.CleanArchitecture.Application.Common.Exceptions;

namespace SwiftDispatch.Sample.CleanArchitecture.Application.Common.Behaviours;

public sealed class LoggingBehaviour<TRequest, TResponse>(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;
        long started = Stopwatch.GetTimestamp();
        logger.LogInformation("Handling {RequestName}", requestName);

        try
        {
            TResponse response = await next();
            logger.LogInformation("Handled {RequestName} in {ElapsedMilliseconds} ms",
                requestName, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            return response;
        }
        catch (RequestValidationException exception)
        {
            logger.LogWarning("Validation failed for {RequestName}: {Reason}", requestName, exception.Message);
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Canceled {RequestName}", requestName);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed {RequestName}", requestName);
            throw;
        }
    }
}
