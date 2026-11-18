# Dispatch catalog

The opt-in dispatch catalog helps you find a message's concrete handler and inspect its configured pipeline without sending a request. It supports requests, void requests, streams, and notifications.

## Enable the catalog

```csharp
services.AddSwiftDispatch(options =>
{
    options.RegisterServicesFromAssembly(typeof(HelloHandler).Assembly);
    options.EnableDispatchCatalog(true);
});
```

The option accepts a compile-time Boolean constant, defaults to false, and uses the last supplied value. When disabled, no catalog implementation or catalog service registration is generated.

## Inspect a configured flow

```csharp
using SwiftDispatch.Diagnostics;

var catalog = provider.GetRequiredService<IDispatchCatalog>();
foreach (var flow in catalog.Find(typeof(Hello)))
{
    Console.WriteLine($"{flow.Kind}: {flow.MessageType} -> {flow.ResponseType}");
    foreach (var behavior in flow.BehaviorTypes)
        Console.WriteLine($"  Behavior: {behavior}");
    foreach (var handler in flow.HandlerTypes)
        Console.WriteLine($"  Handler: {handler}");
}
```

`Entries` exposes all flows sorted by fully qualified message name, then dispatch kind. `Find` matches the exact message type, returns an empty collection when absent, and rejects null. The catalog is a singleton; all collections are read-only and defensively copied. Resolving or reading it does not construct handlers, behaviors, or notification strategies.

Behaviors are closed concrete types in outermost-first order. Void requests have a null response type, even though their behaviors use `Unit`. Streams have their own behaviors. Notifications have no response or behaviors and list all subscribers with the configured notification strategy. Subscriber list order is registration order: only the built-in sequential strategy promises execution in that order. Parallel and custom strategies determine their own execution order.

Only messages present in the generated dispatch table appear. Intentionally unhandled requests and notifications without subscribers are absent. The catalog follows the dispatch table's existing first-registration ownership; it does not aggregate separate host registrations. Notification strategy metadata describes configuration, not a later replacement of the strategy service in DI.

## Find generated flow documentation

Build your project, then expand the SwiftDispatch source generator under your IDE's analyzer/generated-document nodes and open `SwiftDispatch.DispatchCatalog.g.cs`. Search for the fully qualified message name to see its response, ordered behaviors, handlers, and strategy. Available source file paths and one-based line numbers appear in comments; referenced assemblies may show `source location unavailable`.

To inspect generated files on disk, add this to the consuming project:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)generated/$(Configuration)/$(TargetFramework)</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

Build and find `SwiftDispatch.DispatchCatalog.g.cs` under the project's `obj` directory. Keep generated output inside `obj` so it is not compiled twice. Source paths appear only in generated comments, not in runtime catalog values.

This is a configured flow, not an execution trace. Short-circuiting, retries, nested dispatches, and runtime service overrides can change what actually executes. This release does not add IDE navigation commands, caller discovery, a web UI, or runtime tracing.
