using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using SwiftDispatch.Contracts;
using SwiftDispatch.EntityFrameworkCore;
using Xunit;

namespace SwiftDispatch.EntityFrameworkCore.Tests;

public sealed class TransactionTests
{
    [Fact]
    public async Task ResponseAndVoidCommandsCommitAndQueriesBypassTransaction()
    {
        await using var app = await TestApplication.Create();
        int id = await app.Mediator.Send(new CreateItem("first"));
        Assert.True(id > 0);
        await app.Mediator.Send(new RenameItem(id, "updated"));
        Assert.Equal(1, await app.Mediator.Send(new CountItems()));
        Assert.Equal("updated", await app.Context.Items.Select(item => item.Name).SingleAsync());
        Assert.Null(app.Context.Database.CurrentTransaction);
    }

    [Fact]
    public async Task UnmarkedCommandsDoNotOpenTransactions()
    {
        await using var app = await TestApplication.Create();
        Assert.False(await app.Mediator.Send(new UnmarkedCommand()));
    }

    [Fact]
    public async Task LegacyRequestsCanOptIntoTransactions()
    {
        await using var app = await TestApplication.Create();
        Assert.True(await app.Mediator.Send(new LegacyRequest()));
    }

    [Fact]
    public async Task FailureAfterSaveRollsBackAndPreservesException()
    {
        await using var app = await TestApplication.Create();
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => app.Mediator.Send(new CreateItem("bad", true)));
        Assert.Same(app.State.Failure, exception);
        Assert.Equal(0, await app.PersistedCount());
        Assert.Null(app.Context.Database.CurrentTransaction);
    }

    [Fact]
    public async Task NestedSuccessJoinsSameTransaction()
    {
        await using var app = await TestApplication.Create();
        await app.Mediator.Send(new NestedCommand(false));
        Assert.Equal(2, await app.PersistedCount());
        Assert.Equal(2, app.State.TransactionIds.Count);
        Assert.Equal(app.State.TransactionIds[0], app.State.TransactionIds[1]);
    }

    [Fact]
    public async Task CaughtNestedFailureStillPreventsOuterCommit()
    {
        await using var app = await TestApplication.Create();
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => app.Mediator.Send(new NestedCommand(true)));
        Assert.Contains("nested transactional request failed", exception.Message);
        Assert.Equal(0, await app.PersistedCount());
    }

    [Fact]
    public async Task ExistingExternalTransactionIsNeitherCommittedNorDisposed()
    {
        await using var app = await TestApplication.Create();
        await using var transaction = await app.Context.Database.BeginTransactionAsync();
        await app.Mediator.Send(new CreateItem("external"));
        Assert.Same(transaction, app.Context.Database.CurrentTransaction);
        await transaction.RollbackAsync();
        Assert.Equal(0, await app.PersistedCount());
    }

    [Fact]
    public async Task BehaviorDoesNotImplicitlySaveChanges()
    {
        await using var app = await TestApplication.Create();
        var behavior = new EntityFrameworkTransactionBehavior<string, int, StoreContext>(app.Context);
        await behavior.Handle("request", () =>
        {
            app.Context.Items.Add(new Item { Name = "unsaved" });
            return Task.FromResult(1);
        }, default);
        Assert.Equal(0, await app.PersistedCount());
        Assert.True(app.Context.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task PreCancelledRequestDoesNotInvokeHandler()
    {
        await using var app = await TestApplication.Create();
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => app.Mediator.Send(new CreateItem("cancelled"), source.Token));
        Assert.Empty(app.State.TransactionIds);
        Assert.Equal(0, await app.PersistedCount());
    }

    [Fact]
    public async Task CancellationAfterSaveRollsBackEvenWhenHandlerIgnoresCancellation()
    {
        await using var app = await TestApplication.Create();
        using var source = new CancellationTokenSource();
        var behavior = new EntityFrameworkTransactionBehavior<string, int, StoreContext>(app.Context);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => behavior.Handle("request", async () =>
        {
            app.Context.Items.Add(new Item { Name = "cancelled" });
            await app.Context.SaveChangesAsync();
            source.Cancel();
            return 1;
        }, source.Token));
        Assert.Equal(0, await app.PersistedCount());
        Assert.Null(app.Context.Database.CurrentTransaction);
    }

    [Fact]
    public async Task RejectsAutomaticRetryBeforeInvokingHandler()
    {
        await using var app = await TestApplication.Create();
        var options = new DbContextOptionsBuilder<StoreContext>().UseSqlite(app.Connection)
            .ReplaceService<IExecutionStrategyFactory, RetryingStrategyFactory>().Options;
        await using var context = new StoreContext(options);
        var behavior = new EntityFrameworkTransactionBehavior<string, int, StoreContext>(context);
        bool invoked = false;
        await Assert.ThrowsAsync<NotSupportedException>(() => behavior.Handle("request", () =>
        {
            invoked = true;
            return Task.FromResult(1);
        }, default));
        Assert.False(invoked);
        Assert.Null(context.Database.CurrentTransaction);
    }

    [Fact]
    public async Task ExistingTransactionStillRequiresApplicationOwnedRetryStrategy()
    {
        await using var app = await TestApplication.Create();
        var options = new DbContextOptionsBuilder<StoreContext>().UseSqlite(app.Connection)
            .ReplaceService<IExecutionStrategyFactory, RetryingStrategyFactory>().Options;
        await using var context = new StoreContext(options);
        await using var transaction = await context.Database.BeginTransactionAsync();
        var behavior = new EntityFrameworkTransactionBehavior<string, int, StoreContext>(context);
        bool invoked = false;
        await Assert.ThrowsAsync<NotSupportedException>(() => behavior.Handle("request", () =>
        {
            invoked = true;
            return Task.FromResult(1);
        }, default));
        Assert.False(invoked);
        Assert.Same(transaction, context.Database.CurrentTransaction);
    }

    [Fact]
    public async Task RejectsAmbientTransactionBeforeInvokingHandler()
    {
        await using var app = await TestApplication.Create();
        using var ambient = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeAsyncFlowOption.Enabled);
        var behavior = new EntityFrameworkTransactionBehavior<string, int, StoreContext>(app.Context);
        bool invoked = false;
        await Assert.ThrowsAsync<NotSupportedException>(() => behavior.Handle("request", () =>
        {
            invoked = true;
            return Task.FromResult(1);
        }, default));
        Assert.False(invoked);
        Assert.Null(app.Context.Database.CurrentTransaction);
    }

    [Fact]
    public async Task ExplicitApplicationOwnedStrategyCanWrapCompleteOperation()
    {
        await using var app = await TestApplication.Create();
        var options = new DbContextOptionsBuilder<StoreContext>().UseSqlite(app.Connection)
            .ReplaceService<IExecutionStrategyFactory, RetryingStrategyFactory>().Options;
        await using var context = new StoreContext(options);
        var behavior = new EntityFrameworkTransactionBehavior<string, int, StoreContext>(context);
        int response = await context.Database.CreateExecutionStrategy().ExecuteAsync(() =>
            behavior.Handle("request", () => Task.FromResult(7), default));
        Assert.Equal(7, response);
    }
}

public sealed class RetryingStrategyFactory(ExecutionStrategyDependencies dependencies) : IExecutionStrategyFactory
{
    public IExecutionStrategy Create() => new TestRetryStrategy(dependencies);
    private sealed class TestRetryStrategy(ExecutionStrategyDependencies dependencies)
        : ExecutionStrategy(dependencies, 1, TimeSpan.Zero)
    {
        protected override bool ShouldRetryOn(Exception exception) => false;
    }
}

internal sealed class TestApplication(SqliteConnection connection, ServiceProvider provider, AsyncServiceScope scope) : IAsyncDisposable
{
    public SqliteConnection Connection => connection;
    public StoreContext Context => scope.ServiceProvider.GetRequiredService<StoreContext>();
    public IMediator Mediator => scope.ServiceProvider.GetRequiredService<IMediator>();
    public TestState State => scope.ServiceProvider.GetRequiredService<TestState>();

    internal static async Task<TestApplication> Create()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var services = new ServiceCollection();
        services.AddDbContext<StoreContext>(options => options.UseSqlite(connection));
        services.AddScoped<TestState>();
        services.AddSwiftDispatch(options =>
        {
            options.RegisterServicesFromAssembly(typeof(CreateItem).Assembly);
            options.UseEntityFrameworkTransactions<StoreContext>();
        });
        ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        AsyncServiceScope scope = provider.CreateAsyncScope();
        var app = new TestApplication(connection, provider, scope);
        await app.Context.Database.EnsureCreatedAsync();
        return app;
    }

    internal async Task<int> PersistedCount()
    {
        await using AsyncServiceScope fresh = provider.CreateAsyncScope();
        return await fresh.ServiceProvider.GetRequiredService<StoreContext>().Items.CountAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await scope.DisposeAsync();
        await provider.DisposeAsync();
        await connection.DisposeAsync();
    }
}

public sealed class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public sealed class StoreContext(DbContextOptions<StoreContext> options) : DbContext(options)
{
    public DbSet<Item> Items => Set<Item>();
}

public sealed class TestState
{
    public List<Guid> TransactionIds { get; } = [];
    public InvalidOperationException Failure { get; } = new("Handler failed after saving.");
}

[Transactional]
public sealed record CreateItem(string Name, bool Fail = false) : ICommand<int>;
[Transactional(typeof(StoreContext))]
public sealed record RenameItem(int Id, string Name) : ICommand;
[Transactional]
public sealed record NestedCommand(bool CatchFailure) : ICommand;
[Transactional(typeof(StoreContext))]
public sealed record LegacyRequest : IRequest<bool>;
public sealed record UnmarkedCommand : ICommand<bool>;
public sealed record CountItems : IQuery<int>;

public sealed class CreateItemHandler(StoreContext context, TestState state) : IRequestHandler<CreateItem, int>
{
    public async Task<int> Handle(CreateItem request, CancellationToken cancellationToken)
    {
        state.TransactionIds.Add(context.Database.CurrentTransaction!.TransactionId);
        var item = new Item { Name = request.Name };
        context.Items.Add(item);
        await context.SaveChangesAsync(cancellationToken);
        if (request.Fail) throw state.Failure;
        return item.Id;
    }
}

public sealed class RenameItemHandler(StoreContext context) : IRequestHandler<RenameItem>
{
    public async Task Handle(RenameItem request, CancellationToken cancellationToken)
    {
        Item item = await context.Items.SingleAsync(item => item.Id == request.Id, cancellationToken);
        item.Name = request.Name;
        await context.SaveChangesAsync(cancellationToken);
    }
}

public sealed class NestedCommandHandler(ISender sender) : IRequestHandler<NestedCommand>
{
    public async Task Handle(NestedCommand request, CancellationToken cancellationToken)
    {
        await sender.Send(new CreateItem("first"), cancellationToken);
        try
        {
            await sender.Send(new CreateItem("second", request.CatchFailure), cancellationToken);
        }
        catch (InvalidOperationException) when (request.CatchFailure) { }
    }
}

public sealed class CountItemsHandler(StoreContext context) : IRequestHandler<CountItems, int>
{
    public Task<int> Handle(CountItems request, CancellationToken cancellationToken)
    {
        Assert.Null(context.Database.CurrentTransaction);
        return context.Items.CountAsync(cancellationToken);
    }
}

public sealed class UnmarkedCommandHandler(StoreContext context) : IRequestHandler<UnmarkedCommand, bool>
{
    public Task<bool> Handle(UnmarkedCommand request, CancellationToken cancellationToken) => Task.FromResult(context.Database.CurrentTransaction != null);
}

public sealed class LegacyRequestHandler(StoreContext context) : IRequestHandler<LegacyRequest, bool>
{
    public Task<bool> Handle(LegacyRequest request, CancellationToken cancellationToken) => Task.FromResult(context.Database.CurrentTransaction != null);
}
