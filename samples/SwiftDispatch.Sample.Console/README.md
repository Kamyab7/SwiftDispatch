# Console sample

A minimal SwiftDispatch application using generated dependency injection registration,
a request that returns a greeting, and a notification handler that prints the greeting.
The project references the library and source generator directly so it runs against
the source in this repository.

From the repository root, run:

```sh
dotnet run --project samples/SwiftDispatch.Sample.Console
```

Pass a name as the first argument:

```sh
dotnet run --project samples/SwiftDispatch.Sample.Console -- Grace
```

Expected output:

```text
Configured flow: Hello -> HelloHandler
Hello, Grace!
Notification received: Hello, Grace!
```

## Inspect dispatch flows

The sample enables `EnableDispatchCatalog(true)` and prints Hello's configured handler
using `IDispatchCatalog.Find(typeof(Hello))` before sending the request. After building,
open `SwiftDispatch.DispatchCatalog.g.cs` in your IDE's source generator documents
to inspect all configured flows and available source locations. See the
[dispatch catalog guide](../../website/src/docs/dispatch-catalog.md) for details.
