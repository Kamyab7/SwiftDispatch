# Getting started

SwiftDispatch is a free, lightweight mediator targeting **.NET Standard 2.0, .NET 8, and .NET 10**. Source generation handles registration and dispatch without runtime reflection, with support for Native AOT and trimming.

## Installation

Add the packages to your application:

```sh
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package SwiftDispatch
```

The SwiftDispatch package includes the source generator automatically. If a package has not yet been published, build local packages with `bash eng/verify.sh` and use `--source /path/to/SwiftDispatch/artifacts/packages` when installing SwiftDispatch.

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

This requires the `FluentValidation.DependencyInjectionExtensions` package and `using FluentValidation;`. Add your validation pipeline behavior explicitly, as shown in [pipeline behaviors](pipeline-behaviors.md).

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
- Explore the [complete sample](https://github.com/Kamyab7/SwiftDispatch/tree/master/sample/SwiftDispatch.Sample.CleanArchitecture).
