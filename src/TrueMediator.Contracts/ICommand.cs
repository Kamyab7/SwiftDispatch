namespace TrueMediator.Contracts;

/// <summary>Marks commands with or without responses, without selecting a persistence policy.</summary>
public interface ICommandBase;

/// <summary>A command that does not return a response.</summary>
public interface ICommand : IRequest, ICommandBase;

/// <summary>A command that returns a response.</summary>
public interface ICommand<TResponse> : IRequest<TResponse>, ICommandBase;
