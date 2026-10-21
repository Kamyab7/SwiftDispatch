using Microsoft.Extensions.DependencyInjection;
using SwiftDispatch.Contracts;
using SwiftDispatch.Tests.Fixtures;
using SwiftDispatch.Tests.Fixtures.Notifications;
using SwiftDispatch.Tests.Fixtures.Requests;
using Xunit;

namespace SwiftDispatch.Tests.Runtime;

public sealed class DispatchTests
{
    [Fact]
    public async Task ExactConcreteTypesAreUsedAndUnknownNotificationsAreNoOps()
    {
        await using ServiceProvider provider = TestServices.CreateProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Publish(new Unobserved());
        await mediator.Publish(new DerivedNotice());
        Assert.Empty(scope.ServiceProvider.GetRequiredService<State>().Entries);
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(new Unknown()));
        Assert.Contains(nameof(Unknown), exception.Message);
        await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(new DerivedQuery()));
    }

    [Fact]
    public async Task NullInputsAreRejected()
    {
        await using ServiceProvider provider = TestServices.CreateProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await Assert.ThrowsAsync<ArgumentNullException>(() => mediator.Send<int>(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => mediator.Send((Command)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => mediator.Publish(null!));
    }

    [Fact]
    public async Task StructMessagesAreSupported()
    {
        await using ServiceProvider provider = TestServices.CreateProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        Assert.Equal(7, await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new StructQuery(7)));
    }
}
