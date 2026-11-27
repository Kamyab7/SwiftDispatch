# Entity Framework Core transactions

## Software Bill of Materials

For versions released through the SBOM-enabled workflow, download
`TrueMediator.EntityFrameworkCore.<version>.sbom.spdx.json` from the matching
[GitHub Release](https://github.com/Kamyab7/TrueMediator/releases).
This SPDX 2.2 JSON SBOM describes the package and its release build inputs across
supported frameworks, including build dependencies. A consuming application should
generate its own SBOM to inventory the dependency versions it deploys.

`TrueMediator.EntityFrameworkCore` adds opt-in, attribute-selected transactions to response and void requests. The package targets .NET 8, .NET 10, and .NET 11 and depends on EF Core Relational 8.0.34 or later. Core TrueMediator and its command/query contracts do not depend on EF Core.

## Install and configure

Install both TrueMediator and its integration in the application that configures dispatch, plus your EF relational database provider:

```sh
dotnet add package TrueMediator
dotnet add package TrueMediator.EntityFrameworkCore
```

Register your context normally, then select the default context directly in the existing registration lambda:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrueMediator.Contracts;
using TrueMediator.EntityFrameworkCore;

services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));
services.AddTrueMediator(options =>
{
    options.RegisterServicesFromAssembly(typeof(CreateOrder).Assembly);
    options.UseEntityFrameworkTransactions<ApplicationDbContext>();
});

[Transactional]
public sealed record CreateOrder(string Reference) : ICommand<int>;

[Transactional(typeof(ApplicationDbContext))]
public sealed record CancelOrder(int Id) : ICommand;

public sealed record GetOrder(int Id) : IQuery<OrderDto?>;
```

`ApplicationDbContext` and `OrderDto` above are application types. Implement handlers with the existing `IRequestHandler<TRequest,TResponse>` or `IRequestHandler<TRequest>` interfaces, and dispatch them with `mediator.Send(...)`.

The default-context call is a build-time directive, like other TrueMediator options: use it directly in the registration lambda. It does not register the context or start transactions. Only one default may be selected per registration. Explicit attribute context selection overrides that default and can be used without a default. A context must be an accessible, concrete, closed `DbContext` type; register it separately in the same service scope as the mediator and handlers.

No runtime attribute scanning, reflection-based behavior construction, or per-message runtime selection is used. The generator constructs and registers a closed transaction behavior only for each marked request. Do not add `EntityFrameworkTransactionBehavior<,,>` manually as an open behavior.

## Choosing the attribute form

Use `[Transactional]` when requests should use the default configured with `options.UseEntityFrameworkTransactions<ApplicationDbContext>()`. This is the preferred form for an application with one context.

| Attribute | Context selection |
| --- | --- |
| `[Transactional]` | Uses the configured default context. A missing default produces build error `TM010` at the host registration. |
| `[Transactional(typeof(ApplicationDbContext))]` | Explicitly selects `ApplicationDbContext`, overriding any default. Works even when no default is configured. |

In the example above, both forms select the same `ApplicationDbContext`. The explicit form on `CancelOrder` demonstrates an alternative; it is not required for void commands. Both forms support commands with responses and void commands.

Use the explicit form when an application has multiple contexts and a particular request must select a specific one, or when you intentionally configure no default. Select the same scoped context instance used by the handler; selecting another context does not automatically enlist the handler's database operations in its transaction.

The explicit form makes the request declaration reference a concrete context type. Prefer the default form when request contracts should remain separate from that persistence type, and choose the concrete context at the application's composition root. The request still references the integration package for `TransactionalAttribute`.

## Command and query intent

The optional contracts are available in `TrueMediator.Contracts` on all core-supported frameworks, including .NET Standard 2.0:

- `ICommand` inherits the response-less `IRequest` contract.
- `ICommand<TResponse>` inherits `IRequest<TResponse>`.
- Both command forms inherit `ICommandBase`, allowing command-specific custom behaviors.
- `IQuery<TResponse>` inherits `IRequest<TResponse>` and expresses read intent.

They use the existing handlers and sender API; existing `IRequest` implementations continue to work, including explicit transaction opt-in. Commands without `[Transactional]` do not receive a transaction behavior. Query intent does not automatically enable no-tracking, caching, or prevent writes inside a handler. A query can observe an existing transaction if called from a transactional command; it simply does not introduce a boundary of its own.

## Build-time diagnostics

| Diagnostic | Meaning |
| --- | --- |
| `TM009` | `[Transactional]` is applied to a query, stream, notification, non-request, abstract request, or open generic request. |
| `TM010` | The context is missing, invalid, or inaccessible. Configure a default or specify a valid context on the attribute. |
| `TM011` | A concrete request implements both command and query contracts. |
| `TM001` | Default context configuration is repeated or the configuration syntax is unsupported. |

For example, this query fails the build, with the diagnostic attached to its attribute:

```csharp
[Transactional] // TM009: queries cannot be transactional.
public sealed record GetOrder(int Id) : IQuery<OrderDto?>;
```

Inherited query interfaces are recognized. Invalid target declarations and explicit context types are checked in the current assembly even without a dispatch registration, provided the TrueMediator generator is installed. Missing defaults are checked at the host registration so a contracts library can declare `[Transactional]` without choosing an infrastructure context. Included assemblies are also validated; declarations in assemblies outside the registration scope are not inspected. Attributes are single-use and are not inherited from base request classes.

## Pipeline and transaction ownership

The generated transaction behavior is innermost, after explicitly configured request behaviors:

```text
Logging -> Authorization -> Validation -> Transaction -> Handler
```

This lets authorization and validation fail before a transaction starts. Behaviors outside the boundary can still short-circuit, retry, or throw after the transaction has committed; their ordering and retry policy remain application responsibilities.

**Handlers call `SaveChangesAsync` explicitly.** The integration never saves automatically. A handler can save multiple times within the same transaction and obtain database-generated IDs before returning. The owning behavior commits only after the handler completes successfully. A failure or cancellation before commit causes rollback. A normal response representing a business failure is still a successful return; throw or provide a custom policy if it must trigger rollback.

A single `SaveChanges` already provides transactional persistence where supported. An explicit boundary is useful when several operations need to succeed or fail together. See [EF Core transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions).

## Nested requests and failures

Nested marked requests using the same context instance join its current transaction. They do not commit, dispose, or independently save it. A failed nested participant marks the transaction as failed, so the owning behavior rolls back even if an outer handler catches the exception. Subsequent marked requests in that failed transaction are rejected.

If the application created the current transaction, the application remains responsible for committing or rolling it back; the integration cannot intercept an external call to `CommitAsync`. Any failed participant means that external owner must roll back. Ambient `TransactionScope` transactions are rejected.

Different context instances or different context types are independent boundaries. There is no distributed or cross-context atomicity guarantee. Dispatch sequentially within a shared context; EF contexts do not support concurrent operations. Rollback does not reset the change tracker or undo external side effects. After a failure, dispose the affected scope/context before starting another business operation. Rollback is attempted without the cancelled request token, and rollback errors do not replace the original handler exception.

## Execution strategies and retries

The integration never automatically replays handlers. A retrying EF execution strategy without an active application-owned strategy is rejected before handler execution, including when a transaction already exists. An application may wrap the complete operation in an execution strategy, but must arrange safe replay, fresh scopes/contexts where appropriate, and idempotent external effects. Do not blindly wrap mediator dispatch in a retry policy.

A database connection failure during commit can leave the outcome unknown; the integration cannot promise exactly-once effects or that every commit failure means no data was persisted. See [EF Core connection resiliency](https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency).

## Verification and limits

The SQLite sample at `samples/TrueMediator.Sample.EntityFrameworkCore` demonstrates response and void commands, explicit and default context selection, a query, and rollback after a saved change. Run it with:

```sh
dotnet run --project samples/TrueMediator.Sample.EntityFrameworkCore
```

CI verifies real relational transactions and consumes the packed integration on .NET 8, .NET 10, and .NET 11. SQLite is the tested provider; validate other providers and EF major versions with your application. The integration requires a relational provider and does not supply an outbox, durable notifications, sagas, or automatic multi-context coordination.

This EF integration is verified under normal .NET JIT execution. It does not extend TrueMediator's Native AOT guarantee to EF Core, its providers, or application models. The separate EF-free package consumer verifies that command/query contracts and existing dispatch remain trimming/AOT compatible.
