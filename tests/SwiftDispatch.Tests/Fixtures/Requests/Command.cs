using SwiftDispatch.Contracts;

namespace SwiftDispatch.Tests.Fixtures.Requests;

internal sealed class Command : IRequest;

internal sealed class CommandHandler(State state) : IRequestHandler<Command>
{
    public Task Handle(Command request, CancellationToken cancellationToken)
    {
        state.Entries.Add("command");
        return Task.CompletedTask;
    }
}
