using Microsoft.Extensions.DependencyInjection;
using SwiftDispatch.Contracts;
using SwiftDispatch.Tests.Fixtures;
using SwiftDispatch.Tests.Fixtures.Notifications;
using SwiftDispatch.Tests.Fixtures.Requests;
using Xunit;

namespace SwiftDispatch.Tests.Runtime;

public sealed class CancellationTests
{
    [Fact]
    public async Task CancellationIsForwardedAndCheckedAtEachChainStep()
    {
        await using ServiceProvider provider = TestServices.CreateProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        State state = scope.ServiceProvider.GetRequiredService<State>();
        using var cancellation = new CancellationTokenSource();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new Query(), cancellation.Token);
        Assert.Equal(cancellation.Token, state.Token);
        state.Entries.Clear();
        state.CancelInOuter = cancellation;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => mediator.Send(new Query(), cancellation.Token));
        Assert.Equal(["outer.before"], state.Entries);
    }

    [Fact]
    public async Task PreCanceledOperationsDoNotInvokeComponents()
    {
        await using ServiceProvider provider = TestServices.CreateProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => mediator.Send(new Query(), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => mediator.Send(new Command(), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => mediator.Publish(new Notice(), cancellation.Token));
        Assert.Empty(scope.ServiceProvider.GetRequiredService<State>().Entries);
    }
}
