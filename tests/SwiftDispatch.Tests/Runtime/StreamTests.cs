using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using SwiftDispatch.Contracts;
using SwiftDispatch.Tests.Fixtures;
using Xunit;

namespace SwiftDispatch.Tests.Runtime;

public sealed record Numbers(int Count = 3) : IStreamRequest<int>;
public sealed record MissingStream : IStreamRequest<int>;

internal sealed class NumbersHandler(State state) : IStreamRequestHandler<Numbers, int>
{
    public async IAsyncEnumerable<int> Handle(Numbers request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        state.Entries.Add("stream-handler");
        state.Token = cancellationToken;
        try
        {
            for (int i = 0; i < request.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (state.Fail) throw state.Failure;
                await Task.Yield();
                yield return i;
            }
        }
        finally
        {
            state.Entries.Add("stream-disposed");
        }
    }
}

internal sealed class StreamBehavior(State state) : IStreamPipelineBehavior<Numbers, int>
{
    public async IAsyncEnumerable<int> Handle(Numbers request, StreamHandlerDelegate<int> next,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        state.Entries.Add("stream-before");
        try
        {
            if (state.ShortCircuit) yield break;
            await foreach (int value in next().WithCancellation(cancellationToken)) yield return value;
        }
        finally
        {
            state.Entries.Add("stream-after");
        }
    }
}

public sealed class StreamTests
{
    [Fact]
    public async Task StreamsAreLazyRepeatableAndRunThroughPipeline()
    {
        await using var provider = TestServices.CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var state = scope.ServiceProvider.GetRequiredService<State>();
        var stream = scope.ServiceProvider.GetRequiredService<ISender>().CreateStream(new Numbers());
        Assert.Empty(state.Entries);
        for (int attempt = 0; attempt < 2; attempt++)
        {
            List<int> values = [];
            await foreach (int value in stream) values.Add(value);
            Assert.Equal(new[] { 0, 1, 2 }, values);
            Assert.Equal(new[] { "stream-before", "stream-handler", "stream-disposed", "stream-after" }, state.Entries);
            state.Entries.Clear();
        }
    }

    [Fact]
    public async Task EarlyDisposalUnwindsHandlerAndPipeline()
    {
        await using var provider = TestServices.CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        await foreach (int value in scope.ServiceProvider.GetRequiredService<IMediator>().CreateStream(new Numbers())) break;
        Assert.Equal(new[] { "stream-before", "stream-handler", "stream-disposed", "stream-after" }, scope.ServiceProvider.GetRequiredService<State>().Entries);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CancellationFromEitherTokenReachesHandler(bool cancelRequest)
    {
        await using var provider = TestServices.CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        using var requestCancellation = new CancellationTokenSource();
        using var enumerationCancellation = new CancellationTokenSource();
        var stream = scope.ServiceProvider.GetRequiredService<IMediator>().CreateStream(new Numbers(), requestCancellation.Token);
        await using var enumerator = stream.GetAsyncEnumerator(enumerationCancellation.Token);
        Assert.True(await enumerator.MoveNextAsync());
        (cancelRequest ? requestCancellation : enumerationCancellation).Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await enumerator.MoveNextAsync());
        Assert.True(scope.ServiceProvider.GetRequiredService<State>().Token.IsCancellationRequested);
    }

    [Fact]
    public async Task ShortCircuitAndHandlerFailuresArePreserved()
    {
        await using var provider = TestServices.CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var state = scope.ServiceProvider.GetRequiredService<State>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        state.ShortCircuit = true;
        await using (var enumerator = mediator.CreateStream(new Numbers()).GetAsyncEnumerator())
            Assert.False(await enumerator.MoveNextAsync());
        Assert.DoesNotContain("stream-handler", state.Entries);
        state.ShortCircuit = false;
        state.Fail = true;
        await using var failing = mediator.CreateStream(new Numbers()).GetAsyncEnumerator();
        Assert.Same(state.Failure, await Assert.ThrowsAsync<InvalidOperationException>(async () => await failing.MoveNextAsync()));
    }

    [Fact]
    public async Task MissingNullAndPreCancelledRequestsAreRejected()
    {
        await using var provider = TestServices.CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        Assert.Throws<ArgumentNullException>(() => mediator.CreateStream<int>(null!));
        await using var missing = mediator.CreateStream(new MissingStream()).GetAsyncEnumerator();
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await missing.MoveNextAsync());
        await using var cancelled = mediator.CreateStream(new Numbers(), new CancellationToken(true)).GetAsyncEnumerator();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await cancelled.MoveNextAsync());
        Assert.Empty(scope.ServiceProvider.GetRequiredService<State>().Entries);
    }
}
