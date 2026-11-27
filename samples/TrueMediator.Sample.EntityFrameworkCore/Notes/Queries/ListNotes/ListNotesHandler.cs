using Microsoft.EntityFrameworkCore;
using TrueMediator.Contracts;
using TrueMediator.Sample.EntityFrameworkCore.Persistence;

namespace TrueMediator.Sample.EntityFrameworkCore.Notes.Queries.ListNotes;

public sealed class ListNotesHandler(ApplicationDbContext context) : IRequestHandler<ListNotes, string[]>
{
    public Task<string[]> Handle(ListNotes request, CancellationToken cancellationToken)
    {
        if (context.Database.CurrentTransaction != null) throw new InvalidOperationException("Queries must not open a transaction.");
        return context.Notes.AsNoTracking().OrderBy(note => note.Id).Select(note => note.Title).ToArrayAsync(cancellationToken);
    }
}
