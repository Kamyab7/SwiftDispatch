# Is SwiftDispatch right for your project?

SwiftDispatch is a lightweight, MIT-licensed .NET mediator with source-generated registration and dispatch. Use this guide to decide whether to try it in a new project or evaluate an existing application for migration.

## Start with your use case

| You need                        | What to evaluate                                                                                  |
| ------------------------------- | ------------------------------------------------------------------------------------------------- |
| A request routed to one handler | Requests with or without responses, using Task-based handlers                                     |
| Multiple reactions to an event  | Notifications with sequential, parallel, or custom execution strategies                           |
| Results produced over time      | Streaming requests with IAsyncEnumerable and separate stream pipeline behaviors                   |
| Shared application concerns     | Ordered pipeline behaviors you implement for validation, logging, retries, and exception handling |
| Native AOT publishing           | Generated registration and dispatch, with Native AOT support on .NET 8 and .NET 10                |
| Commercial use                  | MIT license; free for personal and commercial projects                                            |

## Check framework and registration requirements

SwiftDispatch targets .NET Standard 2.0, .NET 8, and .NET 10. Native AOT is supported on .NET 8 and .NET 10. Building the repository requires the .NET 10 SDK.

The source generator is included in the SwiftDispatch NuGet package. It reads registration at build time. Use a direct configuration lambda and supported assembly and behavior expressions; dynamic or conditional runtime configuration is unsupported. See [getting started](getting-started.md) for the configuration rules.

## Evaluate an existing MediatR application

Familiar interfaces do not guarantee compatibility with every MediatR integration. Before switching:

- Inventory your request, notification, streaming, and pipeline usage.
- Review application-specific integrations and registration conventions.
- Plan namespace and registration changes with the [migration guide](migration.md).
- Run application tests for pipeline ordering, notification behavior, errors, and cancellation.
- Test trimming and Native AOT publishing if your deployment requires them.

For parallel notifications, handlers share the publication scope. Their dependencies must support concurrent use. See [notification strategies](notifications.md).

## Inspect performance evidence

The repository compares SwiftDispatch, MediatR, and Mediator on .NET 10. The current published results are an exploratory short run with three measurement iterations on a shared development machine. Some repository build and test work overlapped measurement.

These are working examples, not publication-quality evidence. In particular, the yielding async request timing errors overlap substantially, so the results do not establish a reliable async speed winner. Execution time and allocation tradeoffs vary by scenario.

Review [full results and measurement errors](https://github.com/Kamyab7/SwiftDispatch/blob/master/benchmarks/results/short-run.md) and [reproduction instructions](https://github.com/Kamyab7/SwiftDispatch/blob/master/benchmarks/README.md). Run the full job on an idle machine and measure representative application workloads before drawing performance conclusions.

## Try it on one feature

Use the [quick start](getting-started.md) to run a request and handler. Then inspect the [clean architecture sample](https://github.com/Kamyab7/SwiftDispatch/tree/master/samples/SwiftDispatch.Sample.CleanArchitecture) for a larger application structure.

If your application already uses MediatR, begin with the [migration guide](migration.md).
