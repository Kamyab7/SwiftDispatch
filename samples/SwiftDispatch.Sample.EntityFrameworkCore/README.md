# EF Core transaction sample

Run `dotnet run --project samples/SwiftDispatch.Sample.EntityFrameworkCore` from the repository root.

The sample uses an in-memory SQLite database, so it needs no external database service. It demonstrates `[Transactional]`, explicit context selection, command/query contracts, response and void handlers, explicit saving, and rollback after saving. A fresh scope verifies persisted data after a failed command.

`AddNote` uses `[Transactional]` and `RenameNote` uses `[Transactional(typeof(ApplicationDbContext))]` to demonstrate both forms. Both select the same configured `ApplicationDbContext`; the explicit type is not required for a void command. Prefer the short form with one default context, and use an explicit type to override the default or select a context when no default is configured.

See the [EF integration guide](../../website/src/docs/entity-framework-core.md) for transaction ownership, diagnostics, nested dispatch, retry policy, and compatibility limits.

## Structure

- `Program.cs`: application setup and demonstration flow.
- `DependencyInjection.cs`: context and generated dispatch registration.
- `Entities/Note.cs`: persisted entity.
- `Persistence/ApplicationDbContext.cs`: EF Core context.
- `Notes/Commands/`: command contracts and their handlers, grouped by operation.
- `Notes/Queries/`: query contracts and their handlers, grouped by operation.

Each request, handler, entity, and context has its own file, following the feature folders used in the Clean Architecture sample. The packaged consumer links these source files while excluding generated build output.
