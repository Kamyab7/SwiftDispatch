# Pipeline behaviors

Pipeline behaviors wrap request handlers. Use them to compose application-specific validation, authentication, logging, retries, or short-circuiting.

These are custom behaviors you implement; TrueMediator does not automatically validate or authenticate requests. For a maintained validation behavior, opt into the [FluentValidation integration](fluentvalidation.md).

For optional attribute-selected database transactions, see the [EF Core integration](entity-framework-core.md). Its generated transaction boundary runs after your explicitly configured behaviors.

## Define a behavior

Implement `IPipelineBehavior<TRequest, TResponse>`. Call `next()` to continue the pipeline, or return a response directly to short-circuit it.

```csharp
using TrueMediator.Contracts;

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

Use `AddOpenBehavior` for open generic behaviors, or `AddPipelineBehavior` for open or closed behaviors. Behaviors are ordered **outermost first**.

Add registration to the Application-layer `AddApplicationServices` extension:

```csharp
using System.Reflection;

// Inside AddApplicationServices:
return services.AddTrueMediator(options =>
{
    options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    options.AddOpenBehavior(typeof(LoggingBehavior<,>));
});
```

With validation, authentication, and logging registered in that order, a request enters validation first, then authentication, then logging, then its handler. After the handler returns, the pipeline unwinds in reverse order.

## Supported behavior types

You can configure closed behaviors or open two-parameter behaviors. See the [package smoke test behaviors](https://github.com/TrueMediator/TrueMediator/tree/master/tests/PackageSmoke/Behaviors) for working examples.

Return to [getting started](getting-started.md).
