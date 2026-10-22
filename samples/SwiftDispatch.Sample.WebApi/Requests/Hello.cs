using SwiftDispatch.Contracts;

namespace SwiftDispatch.Sample.WebApi.Requests;

public sealed record Hello(string Name) : IRequest<string>;

public sealed class HelloHandler(ILogger<HelloHandler> logger) : IRequestHandler<Hello, string>
{
    public Task<string> Handle(Hello request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Greeting {Name}", request.Name);
        return Task.FromResult($"Hello, {request.Name}!");
    }
}
