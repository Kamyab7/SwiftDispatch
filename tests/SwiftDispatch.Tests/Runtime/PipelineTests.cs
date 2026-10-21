using Microsoft.Extensions.DependencyInjection;
using SwiftDispatch.Contracts;
using SwiftDispatch.Tests.Fixtures;
using SwiftDispatch.Tests.Fixtures.Requests;
using Xunit;

namespace SwiftDispatch.Tests.Runtime;

public sealed class PipelineTests
{
    [Fact]
    public async Task ChainsBehaviorsAndUnwindsInOrder()
    {
        await using ServiceProvider provider = TestServices.CreateProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        Assert.Equal(42, await mediator.Send(new Query()));
        Assert.Equal(["outer.before", "inner.before", "handler", "inner.after", "outer.after"], scope.ServiceProvider.GetRequiredService<State>().Entries);
    }

    [Fact]
    public async Task ResponseLessRequestsUseUnitPipeline()
    {
        await using ServiceProvider provider = TestServices.CreateProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new Command());
        Assert.Equal(["outer.before", "inner.before", "command", "inner.after", "outer.after"], scope.ServiceProvider.GetRequiredService<State>().Entries);
    }

    [Fact]
    public async Task PipelineCanShortCircuit()
    {
        await using ServiceProvider provider = TestServices.CreateProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<State>().ShortCircuit = true;
        Assert.Equal(99, await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new Query()));
        Assert.Empty(scope.ServiceProvider.GetRequiredService<State>().Entries);
    }

    [Fact]
    public async Task RetryInvokesTheSameContinuationAgain()
    {
        await using ServiceProvider provider = TestServices.CreateProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        State state = scope.ServiceProvider.GetRequiredService<State>();
        state.Retry = true;
        Assert.Equal(42, await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new Query()));
        Assert.Equal(2, state.Attempts);
        Assert.Equal(2, state.Entries.Count(static item => item == "inner.before"));
    }

    [Fact]
    public async Task ExceptionsArePreservedAndBehaviorsCanCatchThem()
    {
        await using ServiceProvider provider = TestServices.CreateProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        State state = scope.ServiceProvider.GetRequiredService<State>();
        state.Fail = true;
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<IMediator>().Send(new Query()));
        Assert.Same(state.Failure, exception);
        Assert.True(state.Caught);
    }
}
