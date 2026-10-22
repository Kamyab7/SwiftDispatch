# Migrating from MediatR

SwiftDispatch uses familiar mediator interfaces, including `IMediator`, `IRequest`, `IRequestHandler`, `INotification`, and `IPipelineBehavior`.

## Check your requirements

SwiftDispatch targets .NET 10. Review the supported features before migrating: requests with or without responses, sequential notifications, ordered pipelines, cancellation token forwarding, and generated handler discovery.

Migration guidance applies to these supported features; check application-specific MediatR integrations separately.

## Update registration

1. Add the SwiftDispatch package.
2. Change relevant namespaces to `SwiftDispatch.Contracts`.
3. Replace mediator registration with `services.AddSwiftDispatch()`.
4. Add a top-level, non-generic static partial class with `[GenerateSwiftDispatch]`.
5. Configure pipeline behaviors explicitly in the attribute.
6. Run your application tests, including notification ordering and pipeline behavior.

See [getting started](getting-started.md) and [pipeline behaviors](pipeline-behaviors.md).

## Include other assemblies

Use marker types to include public handlers in referenced assemblies:

```csharp
[GenerateSwiftDispatch(
    HandlerAssemblyMarkers = new[] { typeof(OtherAssemblyMarker) })]
internal static partial class DispatchRegistration;
```

The marker type must be accessible from your application. Handlers in the current project are discovered automatically.
