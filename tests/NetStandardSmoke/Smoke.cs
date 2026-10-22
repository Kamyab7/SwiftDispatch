using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using SwiftDispatch.Contracts;

namespace SwiftDispatch.NetStandardSmoke;

[GenerateSwiftDispatch(StreamPipelineBehaviors = new[] { typeof(StreamBehavior<,>) })]
internal static partial class Registration;

public sealed class Ping : IRequest<int>;

public sealed class PingHandler : IRequestHandler<Ping, int>
{
    public Task<int> Handle(Ping request, CancellationToken cancellationToken) => Task.FromResult(42);
}

public sealed class Numbers : IStreamRequest<int>;

public sealed class NumbersHandler : IStreamRequestHandler<Numbers, int>
{
    public async IAsyncEnumerable<int> Handle(Numbers request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        yield return 1;
        yield return 2;
    }
}

public sealed class StreamBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken cancellationToken) => next();
}

public static class Smoke
{
    public static async Task Run()
    {
        var services = new ServiceCollection();
        services.AddSwiftDispatch();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        if (await mediator.Send(new Ping()) != 42) throw new InvalidOperationException("Request failed.");
        int sum = 0;
        await foreach (int value in mediator.CreateStream(new Numbers())) sum += value;
        if (sum != 3) throw new InvalidOperationException("Stream failed.");
    }
}
