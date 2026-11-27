using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Exporters;
using Microsoft.Extensions.DependencyInjection;

namespace TrueMediator.Benchmarks;

[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[MarkdownExporterAttribute.GitHub]
[JsonExporterAttribute.Full]
[HideColumns(Column.RatioSD)]
public class DispatchBenchmarks
{
    private ServiceProvider _provider = null!;
    private IServiceScope _scope = null!;
    private MediatR.IMediator _mediatR = null!;
    private global::Mediator.IMediator _mediator = null!;
    private TrueMediator.Contracts.IMediator _trueMediator = null!;
    private readonly Request _completed = new(false);
    private readonly Request _yielding = new(true);
    private readonly Notification _notification = new();
    private readonly NotificationCounter _counter = new();
    public int NotificationCount => _counter.Count;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_counter);
        services.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        services.AddMediatR(options =>
        {
            options.RegisterServicesFromAssemblyContaining<Request>();
            options.Lifetime = ServiceLifetime.Transient;
        });
        services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Transient);
        services.AddTrueMediator(options => options.AddHandlersFromAssemblyContaining<Request>());
        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        _scope = _provider.CreateScope();
        _mediatR = _scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
        _mediator = _scope.ServiceProvider.GetRequiredService<global::Mediator.IMediator>();
        _trueMediator = _scope.ServiceProvider.GetRequiredService<TrueMediator.Contracts.IMediator>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    [Benchmark(Baseline = true), BenchmarkCategory("CompletedRequest")]
    public Task<int> MediatR_Send() => _mediatR.Send(_completed);
    [Benchmark, BenchmarkCategory("CompletedRequest")]
    public ValueTask<int> Mediator_Send() => _mediator.Send(_completed);
    [Benchmark, BenchmarkCategory("CompletedRequest")]
    public Task<int> TrueMediator_Send() => _trueMediator.Send(_completed);

    [Benchmark(Baseline = true), BenchmarkCategory("YieldingRequest")]
    public Task<int> MediatR_AsyncSend() => _mediatR.Send(_yielding);
    [Benchmark, BenchmarkCategory("YieldingRequest")]
    public ValueTask<int> Mediator_AsyncSend() => _mediator.Send(_yielding);
    [Benchmark, BenchmarkCategory("YieldingRequest")]
    public Task<int> TrueMediator_AsyncSend() => _trueMediator.Send(_yielding);

    [Benchmark(Baseline = true), BenchmarkCategory("Notification")]
    public Task MediatR_Publish() => _mediatR.Publish(_notification);
    [Benchmark, BenchmarkCategory("Notification")]
    public ValueTask Mediator_Publish() => _mediator.Publish(_notification);
    [Benchmark, BenchmarkCategory("Notification")]
    public Task TrueMediator_Publish() => _trueMediator.Publish(_notification);
}
