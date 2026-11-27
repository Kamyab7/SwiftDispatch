using TrueMediator.Contracts;
using TrueMediator.EntityFrameworkCore;

namespace TrueMediator.Sample.EntityFrameworkCore.Notes.Commands.AddNote;

[Transactional]
public sealed record AddNote(string Title, bool FailAfterSave = false) : ICommand<int>;
