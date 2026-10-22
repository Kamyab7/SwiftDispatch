# Streaming requests

Use `IStreamRequest<TResponse>` and `IStreamRequestHandler<TRequest, TResponse>` for results produced as `IAsyncEnumerable<TResponse>`.

```csharp
public sealed record Count(int Limit) : IStreamRequest<int>;

public sealed class CountHandler : IStreamRequestHandler<Count, int>
{
    public async IAsyncEnumerable<int> Handle(
        Count request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int value = 1; value <= request.Limit; value++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return value;
        }
    }
}
```

Consume results with `await foreach`:

```csharp
await foreach (int value in mediator.CreateStream(new Count(3), cancellationToken))
{
    Console.WriteLine(value);
}
```

Execution starts when enumeration begins. Keep the dependency injection scope alive until enumeration finishes. Cancellation supplied to `CreateStream` or through `WithCancellation` flows to the handler; stopping enumeration disposes the underlying enumerator.

## Stream pipeline behaviors

Implement `IStreamPipelineBehavior<TRequest, TResponse>` with a `Handle` method returning `IAsyncEnumerable<TResponse>` and accepting `StreamHandlerDelegate<TResponse> next`. Call `next()` to enumerate the next behavior or handler.

Configure behaviors in outermost-first order:

```csharp
[GenerateSwiftDispatch(StreamPipelineBehaviors = new[] { typeof(StreamLoggingBehavior<,>) })]
internal static partial class DispatchRegistration;
```

Stream behaviors are separate from the `PipelineBehaviors` used by `Send`.
