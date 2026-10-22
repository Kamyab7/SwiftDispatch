# Getting started

SwiftDispatch is a free, lightweight mediator for **.NET 10**. Source generation handles registration and dispatch without runtime reflection, with support for Native AOT and trimming.

## Installation

Add the packages to your application:

```sh
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package SwiftDispatch
```

The SwiftDispatch package includes the source generator automatically. If a package has not yet been published, build local packages with `bash eng/verify.sh` and use `--source /path/to/SwiftDispatch/artifacts/packages` when installing SwiftDispatch.

## Register services

```csharp
using Microsoft.Extensions.DependencyInjection;
using SwiftDispatch.Contracts;

var services = new ServiceCollection();
services.AddSwiftDispatch();

// Place this declaration after your top-level statements.
[GenerateSwiftDispatch]
internal static partial class DispatchRegistration;
```

The registration class must be a top-level, non-generic `static partial` class. Handlers in the current project are discovered automatically.

## Send your first request

Define a request and its handler:

```csharp
using SwiftDispatch.Contracts;

public sealed record Hello(string Name) : IRequest<string>;

public sealed class HelloHandler : IRequestHandler<Hello, string>
{
    public Task<string> Handle(Hello request, CancellationToken cancellationToken)
        => Task.FromResult($"Hello, {request.Name}!");
}
```

After registration, build a service provider and send the request before the registration class declaration:

```csharp
await using var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();
var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

string greeting = await mediator.Send(new Hello("Ada"));
Console.WriteLine(greeting);
```

## Next steps

- Learn about [pipeline behaviors](pipeline-behaviors.md).
- Publish [notifications](notifications.md) to multiple handlers.
- Review [migration from MediatR](migration.md).
- Explore the [complete sample](https://github.com/Kamyab7/SwiftDispatch/tree/master/samples/SwiftDispatch.Sample).
