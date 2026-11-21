using SwiftDispatch.Contracts;
using SwiftDispatch.EntityFrameworkCore;

namespace SwiftDispatch.Sample.EntityFrameworkCore.Notes.Commands.AddNote;

[Transactional]
public sealed record AddNote(string Title, bool FailAfterSave = false) : ICommand<int>;
