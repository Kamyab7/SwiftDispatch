using Microsoft.EntityFrameworkCore;
using SwiftDispatch.Contracts;
using SwiftDispatch.Sample.EntityFrameworkCore.Entities;
using SwiftDispatch.Sample.EntityFrameworkCore.Persistence;

namespace SwiftDispatch.Sample.EntityFrameworkCore.Notes.Commands.RenameNote;

public sealed class RenameNoteHandler(ApplicationDbContext context) : IRequestHandler<RenameNote>
{
    public async Task Handle(RenameNote request, CancellationToken cancellationToken)
    {
        if (context.Database.CurrentTransaction == null) throw new InvalidOperationException("Expected generated void transaction boundary.");
        Note note = await context.Notes.SingleAsync(note => note.Id == request.Id, cancellationToken);
        note.Title = request.Title;
        await context.SaveChangesAsync(cancellationToken);
    }
}
