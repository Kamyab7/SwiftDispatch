using Microsoft.Extensions.DependencyInjection;
using SwiftDispatch.Contracts;
using SwiftDispatch.Tests.Fixtures;
using SwiftDispatch.Tests.Fixtures.Notifications;
using Xunit;

namespace SwiftDispatch.Tests.Runtime;

public sealed class NotificationTests
{
    [Fact]
    public async Task ObserversAreSequentialAndStableAndRegistrationIsIdempotent()
    {
        await using ServiceProvider provider = TestServices.CreateProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        State state = scope.ServiceProvider.GetRequiredService<State>();
        using var cancellation = new CancellationTokenSource();
        await scope.ServiceProvider.GetRequiredService<IMediator>().Publish(new Notice(), cancellation.Token);
        Assert.Equal(["a.start", "a.end", "z"], state.Entries);
        Assert.Equal(cancellation.Token, state.Token);
    }

    [Fact]
    public async Task NotificationFailureStopsLaterObservers()
    {
        await using ServiceProvider provider = TestServices.CreateProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        State state = scope.ServiceProvider.GetRequiredService<State>();
        state.Fail = true;
        Assert.Same(state.Failure, await Assert.ThrowsAsync<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<IMediator>().Publish(new Notice())));
        Assert.Equal(["a.start"], state.Entries);
    }

    [Fact]
    public async Task CancellationBetweenObserversStopsPublication()
    {
        await using ServiceProvider provider = TestServices.CreateProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        State state = scope.ServiceProvider.GetRequiredService<State>();
        using var cancellation = new CancellationTokenSource();
        state.CancelInObserver = cancellation;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => scope.ServiceProvider.GetRequiredService<IMediator>().Publish(new Notice(), cancellation.Token));
        Assert.Equal(["a.start", "a.end"], state.Entries);
    }
    [Fact]
    public async Task ParallelStrategyStartsLaterHandlersBeforeFirstCompletes()
    {
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new List<int>();
        Task publication = new ParallelNotificationExecutionStrategy().Execute(
        [
            async token => { started.Add(1); await release.Task; },
            token => { started.Add(2); return Task.CompletedTask; }
        ], default);
        Assert.Equal([1, 2], started);
        Assert.False(publication.IsCompleted);
        release.SetResult(true);
        await publication;
    }

    [Fact]
    public async Task ParallelStrategyAwaitsOtherHandlersAfterSynchronousFailure()
    {
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = new InvalidOperationException("failure");
        Task publication = new ParallelNotificationExecutionStrategy().Execute(
        [token => throw failure, token => release.Task], default);
        Assert.False(publication.IsCompleted);
        release.SetResult(true);
        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => publication));
    }

    [Fact]
    public async Task GeneratedDispatcherUsesRegisteredStrategy()
    {
        var services = new ServiceCollection();
        var strategy = new RecordingStrategy();
        services.AddSingleton<INotificationExecutionStrategy>(strategy);
        services.AddScoped<State>();
        services.AddTestDispatch();
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IMediator>().Publish(new Notice());
        Assert.Equal(2, strategy.HandlerCount);
        Assert.Empty(scope.ServiceProvider.GetRequiredService<State>().Entries);
    }

    private sealed class RecordingStrategy : INotificationExecutionStrategy
    {
        public int HandlerCount { get; private set; }

        public Task Execute(IEnumerable<Func<CancellationToken, Task>> handlers, CancellationToken cancellationToken)
        {
            HandlerCount = handlers.Count();
            return Task.CompletedTask;
        }
    }
}
