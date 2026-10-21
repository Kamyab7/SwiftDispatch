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
}
