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
Hello, Grace!
Notification received: Hello, Grace!
```
