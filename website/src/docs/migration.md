# Migrating from MediatR

SwiftDispatch uses familiar mediator interfaces, including `IMediator`, `IRequest`, `IRequestHandler`, `INotification`, and `IPipelineBehavior`.

## Check your requirements

SwiftDispatch targets .NET Standard 2.0, .NET 8, and .NET 10. Review the supported features before migrating: requests with or without responses, sequential notifications, ordered pipelines, cancellation token forwarding, and generated handler discovery.

Migration guidance applies to these supported features; check application-specific MediatR integrations separately.

## Update registration

1. Add the SwiftDispatch package.
2. Change relevant namespaces to `SwiftDispatch.Contracts`.
3. Move registration into an `AddApplicationServices` extension in the Application assembly, using the block-style configuration below.
4. Configure pipeline behaviors explicitly with `AddOpenBehavior(typeof(...))` for open generic behaviors, or `AddPipelineBehavior(typeof(...))` for open or closed behaviors.
5. Run your application tests, including notification ordering and pipeline behavior.

Inside the Application-layer extension:

```csharp
using System.Reflection;

// Inside AddApplicationServices:
return services.AddSwiftDispatch(options =>
{
    options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    options.AddOpenBehavior(typeof(LoggingBehaviour<,>));
    options.AddOpenBehavior(typeof(ValidationBehaviour<,>));
});
```

The behaviors above are examples you implement using SwiftDispatch's `IPipelineBehavior<TRequest, TResponse>`. In the Web project's `Program.cs`, call `builder.Services.AddApplicationServices()`.

See [getting started](getting-started.md) for the complete extension and [pipeline behaviors](pipeline-behaviors.md) for a behavior implementation.

## Include other assemblies

Use a handler type to include public handlers in a referenced assembly:

```csharp
// Inside AddApplicationServices:
return services.AddSwiftDispatch(options =>
{
    options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    options.RegisterServicesFromAssembly(typeof(OtherAssemblyHandler).Assembly);
});
```

Add this assembly call to the same configuration block as your behaviors. The type used in `typeof(...)` must be accessible from your application. Handlers in the current project are discovered automatically.
