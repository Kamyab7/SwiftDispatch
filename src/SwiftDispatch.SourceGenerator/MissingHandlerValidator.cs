using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using static SwiftDispatch.SourceGenerator.SymbolHelpers;

namespace SwiftDispatch.SourceGenerator;

internal static class MissingHandlerValidator
{
    internal static void Validate(Compilation compilation, IEnumerable<IAssemblySymbol> assemblies,
        List<Request> requests, CancellationToken cancellationToken,
        Action<DiagnosticDescriptor, ISymbol, string> reportError)
    {
        var contracts = new[]
        {
            compilation.GetTypeByMetadataName("SwiftDispatch.Contracts.IRequest"),
            compilation.GetTypeByMetadataName("SwiftDispatch.Contracts.IRequest`1"),
            compilation.GetTypeByMetadataName("SwiftDispatch.Contracts.IStreamRequest`1")
        };
        foreach (IAssemblySymbol assembly in assemblies.OrderBy(static item => item.Identity.ToString(), StringComparer.Ordinal))
        {
            foreach (INamedTypeSymbol type in Types(assembly.GlobalNamespace).OrderBy(Name, StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (type.TypeKind is not (TypeKind.Class or TypeKind.Struct) || type.IsAbstract || type.IsStatic || IsOpen(type))
                {
                    continue;
                }

                foreach (INamedTypeSymbol contract in type.AllInterfaces.Where(item => contracts.Any(
                    definition => definition != null && SymbolEqualityComparer.Default.Equals(item.OriginalDefinition, definition))))
                {
                    bool streams = SymbolEqualityComparer.Default.Equals(contract.OriginalDefinition, contracts[2]);
                    bool returnsVoid = SymbolEqualityComparer.Default.Equals(contract, contracts[0]);
                    if (requests.Any(request => SymbolEqualityComparer.Default.Equals(request.Message, type)
                        && request.Streams == streams && request.ReturnsVoid == returnsVoid
                        && (returnsVoid || SymbolEqualityComparer.Default.Equals(request.Response, contract.TypeArguments[0]))))
                    {
                        continue;
                    }

                    reportError(GeneratorDiagnostics.MissingHandler, type,
                        $"Request '{Name(type)}' has no registered handler for '{Name(contract)}'. Add a concrete handler and include its assembly in AddSwiftDispatch registration, or disable this check with ValidateMissingHandlers(false).");
                }
            }
        }
    }
}
