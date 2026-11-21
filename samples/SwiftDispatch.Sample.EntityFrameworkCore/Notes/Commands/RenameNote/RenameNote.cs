using SwiftDispatch.Contracts;
using SwiftDispatch.EntityFrameworkCore;
using SwiftDispatch.Sample.EntityFrameworkCore.Persistence;

namespace SwiftDispatch.Sample.EntityFrameworkCore.Notes.Commands.RenameNote;

[Transactional(typeof(ApplicationDbContext))]
public sealed record RenameNote(int Id, string Title) : ICommand;
