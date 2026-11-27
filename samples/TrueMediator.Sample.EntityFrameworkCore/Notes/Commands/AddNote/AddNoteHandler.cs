using TrueMediator.Contracts;
using TrueMediator.Sample.EntityFrameworkCore.Entities;
using TrueMediator.Sample.EntityFrameworkCore.Persistence;

namespace TrueMediator.Sample.EntityFrameworkCore.Notes.Commands.AddNote;

public sealed class AddNoteHandler(ApplicationDbContext context) : IRequestHandler<AddNote, int>
{
    public async Task<int> Handle(AddNote request, CancellationToken cancellationToken)
    {
        if (context.Database.CurrentTransaction == null) throw new InvalidOperationException("Expected generated transaction boundary.");
        var note = new Note { Title = request.Title };
        context.Notes.Add(note);
        await context.SaveChangesAsync(cancellationToken);
        if (request.FailAfterSave) throw new InvalidOperationException("Failure after saving");
        return note.Id;
    }
}
