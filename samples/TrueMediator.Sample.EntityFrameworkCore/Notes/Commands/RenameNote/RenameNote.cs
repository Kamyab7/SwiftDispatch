using TrueMediator.Contracts;
using TrueMediator.EntityFrameworkCore;
using TrueMediator.Sample.EntityFrameworkCore.Persistence;

namespace TrueMediator.Sample.EntityFrameworkCore.Notes.Commands.RenameNote;

[Transactional(typeof(ApplicationDbContext))]
public sealed record RenameNote(int Id, string Title) : ICommand;
