# Pipeline behaviors

Pipeline behaviors wrap request handlers. Use them to compose application-specific validation, authentication, logging, retries, or short-circuiting.

These are custom behaviors you implement; SwiftDispatch does not automatically validate or authenticate requests.

## Define a behavior

Implement `IPipelineBehavior<TRequest, TResponse>`. Call `next()` to continue the pipeline, or return a response directly to short-circuit it.

```csharp
using SwiftDispatch.Contracts;

public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Handling {typeof(TRequest).Name}");
        TResponse response = await next().ConfigureAwait(false);
        Console.WriteLine($"Handled {typeof(TRequest).Name}");
        return response;
    }
}
```

## Configure the order

Set `PipelineBehaviors` on your registration attribute. Behaviors are ordered **outermost first**.

```csharp
[GenerateSwiftDispatch(
    PipelineBehaviors = new[] { typeof(LoggingBehavior<,>) })]
internal static partial class DispatchRegistration;
```

With validation, authentication, and logging registered in that order, a request enters validation first, then authentication, then logging, then its handler. After the handler returns, the pipeline unwinds in reverse order.

## Supported behavior types

You can configure closed behaviors or open two-parameter behaviors. See the [sample behaviors](https://github.com/Kamyab7/SwiftDispatch/tree/master/samples/SwiftDispatch.Sample.Console/Behaviors) for working examples.

Return to [getting started](getting-started.md).
