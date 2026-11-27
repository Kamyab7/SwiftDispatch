namespace TrueMediator.Contracts;

/// <summary>Expresses read intent. Query handlers must enforce their own read-only behavior.</summary>
public interface IQuery<TResponse> : IRequest<TResponse>;
