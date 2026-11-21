# Getting started

SwiftDispatch is a free, lightweight mediator targeting **.NET Standard 2.0, .NET 8, .NET 10, and .NET 11**. Source generation handles registration and dispatch without runtime reflection, with support for Native AOT and trimming.

## Installation

For a standalone console example, create a project first:

```sh
dotnet new console -n SwiftDispatchDemo
cd SwiftDispatchDemo
```

Add the packages to your application:

```sh
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package SwiftDispatch
```

The SwiftDispatch package includes the source generator automatically and references SwiftDispatch.Contracts.

## Run your first handler

Replace the console project's `Program.cs` with this complete example:

```csharp
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SwiftDispatch;
using SwiftDispatch.Contracts;

var services = new ServiceCollection();
services.AddSwiftDispatch(options =>
{
    options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
});

await using var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();
var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

Console.WriteLine(await mediator.Send(new Hello("Ada")));

public sealed record Hello(string Name) : IRequest<string>;

public sealed class HelloHandler : IRequestHandler<Hello, string>
{
    public Task<string> Handle(Hello request, CancellationToken cancellationToken)
        => Task.FromResult($"Hello, {request.Name}!");
}
```

Run it:

```sh
dotnet run
```

Expected output: `Hello, Ada!`

You have defined an intent, implemented its handler, and dispatched a request. Next, use the Application-layer registration below for a multi-project application, or explore [pipeline behaviors](pipeline-behaviors.md).

## Register services in the Application layer

Create `DependencyInjection.cs` in the project containing your handlers:

```csharp
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace MyApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        return services.AddSwiftDispatch(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });
    }
}
```

Then call your Application-layer extension from the Web project's `Program.cs`:

```csharp
using MyApp.Application;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationServices();
```

`Assembly.GetExecutingAssembly()` identifies the Application assembly because the registration method is defined there. It includes all its handlers; no per-handler registration or marker class is needed. To include public handlers from a referenced assembly, use `typeof(SomeHandler).Assembly`.

The source generator reads the configuration at build time; the lambda is not executed at runtime. Use one direct registration lambda per assembly, with calls on its options parameter. Assemblies must use `Assembly.GetExecutingAssembly()` or `typeof(T).Assembly`; behavior arguments must use `typeof(...)`. Variables, helper calls inside the lambda, and conditional configuration are unsupported.

If you use FluentValidation, register all Application validators inside `AddApplicationServices`, before `AddSwiftDispatch`:

```csharp
services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
```

This scanning helper requires the `FluentValidation.DependencyInjectionExtensions` package and `using FluentValidation;`. For Native AOT, prefer explicit validator registration. Install `SwiftDispatch.FluentValidation` and add its behavior explicitly as shown in the [FluentValidation integration guide](fluentvalidation.md).

## Validate missing handlers at build time

By default, SwiftDispatch reports build error `SD007` for every concrete, closed
request type without a matching registered handler in the current assembly or any
assembly explicitly included in the registration. This includes unused requests,
requests without responses, and streaming requests. Handlers match exact concrete
types, so a handler for a base request does not cover derived requests. Notifications
can have zero handlers.

If some requests are intentionally unhandled, turn off this check:

```csharp
services.AddSwiftDispatch(options =>
{
    options.RegisterServicesFromAssembly(typeof(SomeHandler).Assembly);
    options.ValidateMissingHandlers(false);
});
```

Pass a compile-time Boolean constant; `true` explicitly enables validation. Other
generator errors remain enabled. With validation disabled, sending an unhandled
request still throws `InvalidOperationException` at runtime.

Interfaces, abstract types, open generic request definitions, and types in assemblies
outside this registration's scope are excluded. The check does not analyze `Send`
call sites or verify runtime dependency injection registrations. For separate
contracts and handler projects, include both assemblies in the registration to
validate all contract request types.

Notification handler validation is separate and **disabled by default**, because
notifications can legitimately have no subscribers. Opt in to build error `SD008`
when a concrete, closed notification in the same validation scope has no registered
handler:

```csharp
services.AddSwiftDispatch(options =>
{
    options.RegisterServicesFromAssembly(typeof(SomeHandler).Assembly);
    options.ValidateNotificationHandlers(true);
});
```

At least one handler is required when enabled; multiple handlers remain valid.
Pass `false` to disable this check. Both validation options accept compile-time
Boolean constants and operate independently. Notification handlers must match the
exact concrete notification type, consistent with runtime dispatch.

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

To try the Application-layer registration outside the Web host, create a service collection and send the request:

```csharp
using Microsoft.Extensions.DependencyInjection;
using MyApp.Application;
using SwiftDispatch.Contracts;

var services = new ServiceCollection();
services.AddApplicationServices();

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
- Explore the [complete sample](https://github.com/Kamyab7/SwiftDispatch/tree/master/samples/SwiftDispatch.Sample.CleanArchitecture).
