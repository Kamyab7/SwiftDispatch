using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SwiftDispatch.Contracts;
using SwiftDispatch.Sample.EntityFrameworkCore;
using SwiftDispatch.Sample.EntityFrameworkCore.Notes.Commands.AddNote;
using SwiftDispatch.Sample.EntityFrameworkCore.Notes.Commands.RenameNote;
using SwiftDispatch.Sample.EntityFrameworkCore.Notes.Queries.ListNotes;
using SwiftDispatch.Sample.EntityFrameworkCore.Persistence;

await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
var services = new ServiceCollection();
services.AddNotesServices(connection);
await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
await using (AsyncServiceScope scope = provider.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreatedAsync();
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
    int id = await mediator.Send(new AddNote("Initial title"));
    if (id <= 0) throw new InvalidOperationException("The handler must receive its generated ID before returning.");
    await mediator.Send(new RenameNote(id, "Saved title"));
    try
    {
        await mediator.Send(new AddNote("Rolled back", FailAfterSave: true));
        throw new InvalidOperationException("The failing command unexpectedly succeeded.");
    }
    catch (InvalidOperationException exception) when (exception.Message == "Failure after saving")
    {
        Console.WriteLine("Failed command rolled back.");
    }
}
// Use a fresh scope after a failure: rollback does not reset EF's change tracker.
await using (AsyncServiceScope scope = provider.CreateAsyncScope())
{
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
    string[] notes = await mediator.Send(new ListNotes());
    if (!notes.SequenceEqual(["Saved title"])) throw new InvalidOperationException("Unexpected persisted notes.");
    Console.WriteLine(string.Join(", ", notes));
}
Console.WriteLine("SwiftDispatch EF package smoke test passed.");
