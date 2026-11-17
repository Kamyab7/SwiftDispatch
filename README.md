# SwiftDispatch

![SwiftDispatch banner](assets/logo/swiftdispatch-banner.png)

A free, lightweight MediatR alternative with Native AOT support.

Supports `netstandard2.0`, `net8.0`, `net10.0`, and `net11.0`. Native AOT is supported on .NET 8, .NET 10, and .NET 11. Building this repository requires the .NET 11 SDK.

## Quick start

Install [SwiftDispatch from NuGet](https://www.nuget.org/packages/SwiftDispatch/1.0.0). The package includes the source generator automatically and references `SwiftDispatch.Contracts`.

```sh
dotnet add package SwiftDispatch
```

```csharp
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SwiftDispatch;
using SwiftDispatch.Contracts;

var services = new ServiceCollection();
services.AddSwiftDispatch(options =>
{
    // Register once; use typeof(SomeHandler).Assembly for another assembly.
    options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());

    // Optional: run notification handlers in parallel (sequential by default).
    // Parallel handlers share a scope; dependencies must support concurrent use.
    // options.UseNotificationExecutionStrategy<ParallelNotificationExecutionStrategy>();

    // Optional: add your pipeline behaviors in outermost-first order.
    // options.AddOpenBehavior(typeof(LoggingBehaviour<,>));
    // options.AddOpenBehavior(typeof(ValidationBehaviour<,>));
});

await using var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();
var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

string greeting = await mediator.Send(new Hello("Ada"));
Console.WriteLine(greeting); // Hello, Ada!
// Use mediator.Publish(notification) for notifications.

public sealed record Hello(string Name) : IRequest<string>;

public sealed class HelloHandler : IRequestHandler<Hello, string>
{
    public Task<string> Handle(Hello request, CancellationToken cancellationToken)
        => Task.FromResult($"Hello, {request.Name}!");
}
```

## Samples

- [Console app](samples/SwiftDispatch.Sample.Console): a minimal request and notification example. Run `dotnet run --project samples/SwiftDispatch.Sample.Console -- Ada`.
- [Clean architecture](samples/SwiftDispatch.Sample.CleanArchitecture): separate domain, application, infrastructure, and API projects with application pipeline behaviors. Run `dotnet run --project samples/SwiftDispatch.Sample.CleanArchitecture/Web -- --urls http://localhost:5081`.

## Supported features

| Feature | Support |
| --- | --- |
| Requests | Requests with or without responses using `Task`-based handlers |
| Streaming | `IStreamRequest<T>`, `IStreamRequestHandler<TRequest, T>`, and `mediator.CreateStream(...)` with separate stream pipeline behaviors |
| Pipeline behaviors | Ordered execution, short-circuiting, retries, and exception handling |
| Notifications | Multiple handlers with sequential (default), parallel, or custom execution strategies |
| Native AOT and trimming | Source-generated registration and dispatch |
| Dependency injection | Microsoft dependency injection integration and cancellation token forwarding |
| Handler discovery | Explicitly included assemblies and constrained generic pipeline behaviors |

## Build-time handler validation

Missing request handlers fail the build with `SD007` by default. The generator checks
concrete, closed request types in the assembly containing `AddSwiftDispatch` and any
assemblies explicitly included in that registration. This covers requests with or
without responses and streaming requests, including unused requests. Each request
requires a handler for its exact concrete type; a base request's handler does not
handle derived requests. Notifications may have no handlers.

To allow intentionally unhandled requests, disable this check for the registration:

```csharp
services.AddSwiftDispatch(options =>
{
    options.RegisterServicesFromAssembly(typeof(SomeHandler).Assembly);
    options.ValidateMissingHandlers(false);
});
```

The argument must be a compile-time Boolean constant. Disabling this check leaves
other generator diagnostics enabled; sending an unhandled request still throws at
runtime. Abstract types, interfaces, open generic request definitions, and request
types in assemblies outside the registration scope are not checked. This validation
does not inspect `Send` call sites or validate runtime dependency injection services.

## Benchmarks

Speed and managed memory allocations for SwiftDispatch, MediatR, and Mediator on .NET 10.
Lower values are better; results are from an exploratory short run.

| Scenario | Library | Mean time | Allocated per operation |
| --- | --- | ---: | ---: |
| Completed request | MediatR | 47.57 ns | 200 B |
| Completed request | Mediator | 29.20 ns | 88 B |
| Completed request | SwiftDispatch | 25.11 ns | 136 B |
| Notification | MediatR | 215.53 ns | 768 B |
| Notification | Mediator | 36.72 ns | 56 B |
| Notification | SwiftDispatch | 54.70 ns | 184 B |
| Yielding async request | MediatR | 888.31 ns | 224 B |
| Yielding async request | Mediator | 998.18 ns | 192 B |
| Yielding async request | SwiftDispatch | 859.18 ns | 160 B |

See the [full results and measurement errors](benchmarks/results/short-run.md),
[all pairwise comparisons](benchmarks/results/comparison.md), and
[benchmark instructions](benchmarks/README.md) to reproduce the measurements.
Use the full job on an idle machine before drawing performance conclusions.

## Contributing

See [Contributing to SwiftDispatch](CONTRIBUTING.md) for setup, coding conventions, testing, and pull request guidelines.

Please follow our [Code of Conduct](CODE_OF_CONDUCT.md) in all project spaces.

## Security

See our [security policy](SECURITY.md) for supported versions and how to report vulnerabilities privately.

## License

Licensed under [MIT](LICENSE). Free for personal and commercial use.
