# Clean architecture sample

A .NET 10 todo API following the core folder conventions of [Jason Taylor's CleanArchitecture template](https://github.com/jasontaylordev/CleanArchitecture): feature folders for commands and queries, shared application concerns under `Common`, and one dependency registration entry point per layer.

```text
src/
  Domain/
    Entities/TodoItem.cs
  Application/
    Common/
      Behaviours/
        LoggingBehaviour.cs
        ValidationBehaviour.cs
      Exceptions/RequestValidationException.cs
      Interfaces/ITodoRepository.cs
    TodoItems/
      Commands/
        CreateTodo/
          CreateTodo.cs
          CreateTodoHandler.cs
          CreateTodoValidator.cs
        CompleteTodo/
          CompleteTodo.cs
          CompleteTodoHandler.cs
      Queries/
        GetTodo/
          GetTodo.cs
          GetTodoHandler.cs
      TodoResponse.cs
      Notifications/TodoCompleted.cs
      EventHandlers/TodoCompletedEventHandler.cs
    DependencyInjection.cs
  Infrastructure/
    Persistence/InMemoryTodoRepository.cs
    DependencyInjection.cs
  Web/
    Endpoints/
      TodoItems.cs
      CreateTodoBody.cs
    Program.cs
```

Each request and handler has a separate file. Domain has no framework or SwiftDispatch dependencies. Application references Domain, SwiftDispatch, and the logging abstraction; Infrastructure references Application and implements its repository interface. Web composes these layers and translates HTTP requests and responses.

Application owns the pipeline because logging and validation apply to use cases regardless of their caller. `LoggingBehaviour` depends on `ILogger<T>`, an abstraction; Web supplies the logging providers through ASP.NET Core. Application does not depend on ASP.NET Core or a concrete logging provider. Domain retains the title invariant and completion rules.

`src/Application/DependencyInjection.cs` configures the SwiftDispatch source generator and exposes `AddApplicationServices()`. The generator discovers handlers in Application itself. `AddInfrastructureServices()` registers persistence, and Web's `Program.cs` calls both methods before mapping the endpoints.

The host registers the repository as a singleton so data survives across HTTP request scopes. Reads and writes copy domain objects to avoid sharing mutable instances between requests. This store is for demonstration: data is lost on restart, and it does not provide transactions or optimistic concurrency.

## Notifications

`CompleteTodoHandler` publishes `TodoCompleted` through the injected `IPublisher` after saving the completed item. The notification is an application event under `TodoItems/Notifications`; Domain remains independent of SwiftDispatch.

`TodoItems/EventHandlers/TodoCompletedEventHandler.cs` implements `INotificationHandler<TodoCompleted>` and logs the completed item's ID and title. The source generator discovers and registers it automatically. Add more handlers for the same notification to react independently; SwiftDispatch awaits them sequentially.

To try it in Scalar, create a todo, then call `POST /todos/{id}/complete`. Logs show `Handling CompleteTodo`, `Todo completed: ...`, and `Handled CompleteTodo in ... ms`. Repeating completion for an already completed item returns the existing result without saving or publishing again. Missing IDs return `404` without a notification.

Publication runs in process and is awaited before returning the HTTP response. It is separate from persistence: if a notification handler fails, the todo remains saved. The in-memory store does not guarantee exactly-once publication across concurrent requests.

## Pipeline behaviors

`Application/DependencyInjection.cs` selects behaviors in execution order, outermost first:

```text
CreateTodo:   LoggingBehaviour -> ValidationBehaviour -> CreateTodoHandler
GetTodo:      LoggingBehaviour -> ValidationBehaviour -> GetTodoHandler
CompleteTodo: LoggingBehaviour -> ValidationBehaviour -> CompleteTodoHandler
```

The open generic `LoggingBehaviour<,>` in `Application/Common/Behaviours` wraps every request, records elapsed time on success, and logs validation failures, cancellation, or unexpected exceptions before rethrowing. It uses the logging abstraction with providers configured by the host.

The generic `ValidationBehaviour<TRequest, TResponse>` resolves all registered FluentValidation `IValidator<TRequest>` implementations and calls `ValidateAsync` with the request cancellation token before invoking the handler. Validators run sequentially so they can share scoped dependencies. Requests without validators proceed directly to their handlers.

`CreateTodoValidator` lives beside the create request and handler. Its `NotEmpty()` rule rejects missing, empty, and whitespace-only titles. Like Jason Taylor's template, `DependencyInjection.cs` calls `AddValidatorsFromAssembly(Assembly.GetExecutingAssembly())` to discover all public validators in Application and register them with scoped lifetimes. Add new public validators beside their requests; no individual registrations are needed.

The pipeline collects failures from every validator and throws `RequestValidationException`, grouping distinct messages by property name. The API returns `400 Bad Request` with validation problem details, including `errors.Title`. Validation also applies to callers outside HTTP; Domain retains its own title invariant.

For a valid create request, logs show `Handling CreateTodo` followed by `Handled CreateTodo in ... ms`. Try a rejected request:

```sh
curl -i http://localhost:5081/todos \
  -H 'Content-Type: application/json' \
  -d '{"title":" "}'
```

This returns `400` with `errors: {"Title":["Title is required."]}` in the problem details body. Logs show `Handling CreateTodo` followed by a validation failure, without a success log.

Run from the repository root:

```sh
dotnet run --project samples/SwiftDispatch.Sample.CleanArchitecture/src/Web -- --urls http://localhost:5081
```

Open [Scalar](http://localhost:5081/scalar) to try the endpoints interactively. The `http` launch profile sets the Development environment and configures IDEs to open the browser at `scalar`. When using `dotnet run` from a terminal, open the URL manually if your launcher does not open a browser.

In Rider, open `SwiftDispatch.slnx` and select **Clean Architecture (Scalar)** in the run configuration dropdown. The shared configuration in `.run/` targets `src/Web/Web.csproj` and its `http` launch profile. Old configurations for the removed Console or WebApi samples will report a missing project. If the shared configuration is not visible, right-click `src/Web/Properties/launchSettings.json`, choose **Generate Configurations**, and run **Web: http**.

Scalar uses the generated OpenAPI document at `/openapi/v1.json`. Both documentation endpoints are enabled in Development. Create a todo, copy its returned `id`, then use it in the get and complete operations.

Create a todo (returns `201 Created` with a `Location` header):

```sh
curl -i http://localhost:5081/todos \
  -H 'Content-Type: application/json' \
  -d '{"title":"Try clean architecture"}'
```

Use the returned `id` to read and complete it:

```sh
curl http://localhost:5081/todos/<id>
curl -X POST http://localhost:5081/todos/<id>/complete
```

Both return the todo as JSON with `id`, `title`, and `isCompleted`. Completion is idempotent. Unknown IDs return `404 Not Found`; missing or blank titles return `400 Bad Request`. Each endpoint forwards the request cancellation token through the mediator to the repository.

Build all four layers:

```sh
dotnet build samples/SwiftDispatch.Sample.CleanArchitecture/src/Web
```
