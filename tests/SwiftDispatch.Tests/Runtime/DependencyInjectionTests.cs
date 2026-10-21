using Microsoft.Extensions.DependencyInjection;
using SwiftDispatch.Contracts;
using SwiftDispatch.Tests.Fixtures;
using SwiftDispatch.Tests.Fixtures.Requests;
using Xunit;

namespace SwiftDispatch.Tests.Runtime;

public sealed class DependencyInjectionTests
{
    [Fact]
    public async Task AliasesShareMediatorAndScopesDisposeTransientComponents()
    {
        await using ServiceProvider provider = TestServices.CreateProvider();
        DisposableDependency dependency;
        State state;
        IMediator first;
        await using (AsyncServiceScope scope = provider.CreateAsyncScope())
        {
            first = scope.ServiceProvider.GetRequiredService<IMediator>();
            Assert.Same(first, scope.ServiceProvider.GetRequiredService<ISender>());
            Assert.Same(first, scope.ServiceProvider.GetRequiredService<IPublisher>());
            dependency = scope.ServiceProvider.GetRequiredService<DisposableDependency>();
            state = scope.ServiceProvider.GetRequiredService<State>();
            await first.Send(new Query());
            await first.Send(new Query());
            Assert.Equal(2, dependency.HandlersCreated);
            Assert.Equal(0, state.HandlersDisposed);
        }
        Assert.True(dependency.Disposed);
        Assert.Equal(2, state.HandlersDisposed);
        await using AsyncServiceScope second = provider.CreateAsyncScope();
        Assert.NotSame(first, second.ServiceProvider.GetRequiredService<IMediator>());
        Assert.Empty(second.ServiceProvider.GetRequiredService<State>().Entries);
    }
}
