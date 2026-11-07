using System.Collections.Generic;
using Microsoft.CodeAnalysis;


namespace SwiftDispatch.SourceGenerator;

internal sealed class Request(ITypeSymbol message, ITypeSymbol response, INamedTypeSymbol handler, bool returnsVoid, bool streams)
{
    public ITypeSymbol Message { get; } = message;
    public ITypeSymbol Response { get; } = response;
    public INamedTypeSymbol Handler { get; } = handler;
    public bool Streams { get; } = streams;
    public bool ReturnsVoid { get; } = returnsVoid;
    public List<INamedTypeSymbol> Behaviors { get; } = new();
}

internal sealed class Observer(ITypeSymbol message, INamedTypeSymbol handler)
{
    public ITypeSymbol Message { get; } = message;
    public INamedTypeSymbol Handler { get; } = handler;
}
