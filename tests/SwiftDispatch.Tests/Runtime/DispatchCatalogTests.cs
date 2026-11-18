using Microsoft.Extensions.DependencyInjection;
using SwiftDispatch.Contracts;
using SwiftDispatch.Diagnostics;
using SwiftDispatch.Tests.Fixtures;
using SwiftDispatch.Tests.Fixtures.Behaviors;
using SwiftDispatch.Tests.Fixtures.Notifications;
using SwiftDispatch.Tests.Fixtures.Requests;
using Xunit;

namespace SwiftDispatch.Tests.Runtime;

public sealed class DispatchCatalogTests
{
    [Fact]
    public async Task CatalogIsSingletonAndLookupDoesNotConstructApplicationServices()
    {
        var services = new ServiceCollection();
        services.AddTestDispatch();
        services.AddTestDispatch();
        // Deliberately omit all handler and behavior dependencies. Only catalog resolution succeeds.
        await using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IDispatchCatalog>();
        await using var scope = provider.CreateAsyncScope();
        Assert.Same(catalog, scope.ServiceProvider.GetRequiredService<IDispatchCatalog>());
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IDispatchCatalog));
        Assert.NotEmpty(catalog.Entries);
        Assert.Single(catalog.Find(typeof(Query)));
        Assert.Empty(catalog.Find(typeof(DerivedQuery)));
        Assert.Empty(catalog.Find(typeof(Unknown)));
        Assert.Empty(catalog.Find(typeof(Unobserved)));
        Assert.Throws<ArgumentNullException>(() => catalog.Find(null!));
        Assert.Equal(catalog.Entries.OrderBy(entry => entry.MessageType.FullName, StringComparer.Ordinal).ThenBy(entry => entry.Kind), catalog.Entries);
    }

    [Fact]
    public async Task DescriptorsMatchActualPipelineAndDistinguishVoidStreamsAndNotifications()
    {
        await using var provider = TestServices.CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var catalog = provider.GetRequiredService<IDispatchCatalog>();
        var query = Assert.Single(catalog.Find(typeof(Query)));
        Assert.Equal(DispatchKind.Request, query.Kind);
        Assert.Equal(typeof(int), query.ResponseType);
        Assert.Equal(new[] { typeof(QueryHandler) }, query.HandlerTypes);
        Assert.Equal(new[] { typeof(Outer<Query, int>), typeof(Inner<Query, int>) }, query.BehaviorTypes);
        Assert.Null(query.NotificationStrategyType);
        Assert.Equal(42, await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new Query()));
        Assert.Equal(new[] { "outer.before", "inner.before", "handler", "inner.after", "outer.after" }, scope.ServiceProvider.GetRequiredService<State>().Entries);
        var command = Assert.Single(catalog.Find(typeof(Command)));
        Assert.Null(command.ResponseType);
        Assert.Equal(new[] { typeof(Outer<Command, Unit>), typeof(Inner<Command, Unit>) }, command.BehaviorTypes);
        var stream = Assert.Single(catalog.Find(typeof(Numbers)));
        Assert.Equal(DispatchKind.Stream, stream.Kind);
        Assert.Equal(typeof(int), stream.ResponseType);
        Assert.Equal(new[] { typeof(StreamBehavior) }, stream.BehaviorTypes);
        Assert.Equal(new[] { typeof(NumbersHandler) }, stream.HandlerTypes);
        var notification = Assert.Single(catalog.Find(typeof(Notice)));
        Assert.Equal(DispatchKind.Notification, notification.Kind);
        Assert.Null(notification.ResponseType);
        Assert.Empty(notification.BehaviorTypes);
        Assert.Equal(typeof(SequentialNotificationExecutionStrategy), notification.NotificationStrategyType);
        Assert.Equal(new[] { typeof(AObserver), typeof(ZObserver) }, notification.HandlerTypes);
    }

    [Fact]
    public async Task FindReturnsEveryDispatchKindForTheExactMessage()
    {
        await using var provider = TestServices.CreateProvider();
        var flows = provider.GetRequiredService<IDispatchCatalog>().Find(typeof(DualMessage));
        Assert.Equal(new[] { DispatchKind.Request, DispatchKind.Notification }, flows.Select(flow => flow.Kind));
        Assert.All(flows, flow => Assert.Equal(new[] { typeof(DualHandler) }, flow.HandlerTypes));
        Assert.Equal(typeof(int), flows[0].ResponseType);
        Assert.Null(flows[1].ResponseType);
    }

    [Fact]
    public async Task ExposedCollectionsAndConstructorInputsCannotMutateCatalog()
    {
        await using var provider = TestServices.CreateProvider();
        var catalog = provider.GetRequiredService<IDispatchCatalog>();
        var entries = Assert.IsAssignableFrom<IList<DispatchDescriptor>>(catalog.Entries);
        var flow = Assert.Single(catalog.Find(typeof(Query)));
        Assert.Throws<NotSupportedException>(() => entries.Clear());
        Assert.Throws<NotSupportedException>(() => Assert.IsAssignableFrom<IList<DispatchDescriptor>>(catalog.Find(typeof(Query))).Clear());
        Assert.Throws<NotSupportedException>(() => Assert.IsAssignableFrom<IList<Type>>(flow.HandlerTypes).Clear());
        Assert.Throws<NotSupportedException>(() => Assert.IsAssignableFrom<IList<Type>>(flow.BehaviorTypes).Clear());
        Type[] behaviors = [typeof(string)];
        var handlers = new List<Type> { typeof(QueryHandler) };
        var descriptor = new DispatchDescriptor(typeof(Query), typeof(int), DispatchKind.Request, behaviors, handlers, null);
        behaviors[0] = typeof(int);
        handlers.Clear();
        Assert.Equal(new[] { typeof(string) }, descriptor.BehaviorTypes);
        Assert.Equal(new[] { typeof(QueryHandler) }, descriptor.HandlerTypes);
        Assert.Throws<ArgumentNullException>(() => new DispatchDescriptor(null!, null, DispatchKind.Request, [], [], null));
        Assert.Throws<ArgumentNullException>(() => new DispatchDescriptor(typeof(Query), null, DispatchKind.Request, null!, [], null));
        Assert.Throws<ArgumentNullException>(() => new DispatchDescriptor(typeof(Query), null, DispatchKind.Request, [], null!, null));
        Assert.Throws<ArgumentException>(() => new DispatchDescriptor(typeof(Query), null, DispatchKind.Request, [], new Type[] { null! }, null));
    }
}

internal sealed record DualMessage : IRequest<int>, INotification;

internal sealed class DualHandler : IRequestHandler<DualMessage, int>, INotificationHandler<DualMessage>
{
    Task<int> IRequestHandler<DualMessage, int>.Handle(DualMessage request, CancellationToken cancellationToken) => Task.FromResult(1);
    Task INotificationHandler<DualMessage>.Handle(DualMessage notification, CancellationToken cancellationToken) => Task.CompletedTask;
}
