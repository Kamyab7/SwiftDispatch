# ASP.NET Core Web API sample

This .NET 10 minimal API registers SwiftDispatch with the source generator and resolves a scoped mediator through endpoint dependency injection. The greeting handler uses ASP.NET Core logging, and the endpoint forwards the request cancellation token to the mediator.

`DispatchRegistration` configures the open generic `LoggingBehavior<,>` pipeline behavior. It wraps each request handler, logs before and after successful execution, and logs exceptions before rethrowing them. Calling the greeting endpoint produces `Handling Hello`, `Greeting Ada`, and `Handled Hello` in order.

Run from the repository root:

```sh
dotnet run --project samples/SwiftDispatch.Sample.WebApi -- --urls http://localhost:5080
```

Call the endpoint:

```sh
curl http://localhost:5080/greetings/Ada
```

Response:

```json
{"greeting":"Hello, Ada!"}
```
