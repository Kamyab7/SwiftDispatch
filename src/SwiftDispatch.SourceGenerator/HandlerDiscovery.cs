using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using static SwiftDispatch.SourceGenerator.SymbolHelpers;
using static SwiftDispatch.SourceGenerator.GeneratorDiagnostics;

namespace SwiftDispatch.SourceGenerator;

internal static class HandlerDiscovery
{
    internal static (List<Request> Requests, List<Observer> Notifications) Discover(
        IEnumerable<IAssemblySymbol> assemblies, INamedTypeSymbol responseHandler,
        INamedTypeSymbol streamHandler, INamedTypeSymbol voidHandler,
        INamedTypeSymbol notificationHandler, INamedTypeSymbol unit,
        System.Threading.CancellationToken cancellationToken,
        Action<DiagnosticDescriptor, ISymbol, string> reportError)
    {
        var requests = new List<Request>();
        var notifications = new List<Observer>();
        foreach (IAssemblySymbol assembly in assemblies.OrderBy(static item => item.Identity.ToString(), StringComparer.Ordinal))
        {
            foreach (INamedTypeSymbol type in Types(assembly.GlobalNamespace).OrderBy(Name, StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (type.TypeKind != TypeKind.Class || type.IsAbstract || type.IsStatic)
                {
                    continue;
                }

                foreach (INamedTypeSymbol implemented in type.AllInterfaces)
                {
                    bool returnsResponse = SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, responseHandler);
                    bool streams = SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, streamHandler);
                    bool returnsVoid = SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, voidHandler);
                    bool observes = SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, notificationHandler);
                    if (!returnsResponse && !returnsVoid && !observes && !streams)
                    {
                        continue;
                    }

                    if (IsOpen(type))
                    {
                        reportError(OpenHandler, type, $"Handler '{Name(type)}' is open generic. Supply a concrete, closed handler class.");
                        continue;
                    }

                    if (observes)
                    {
                        notifications.Add(new Observer(implemented.TypeArguments[0].WithNullableAnnotation(NullableAnnotation.NotAnnotated), type));
                    }
                    else
                    {
                        requests.Add(new Request(implemented.TypeArguments[0].WithNullableAnnotation(NullableAnnotation.NotAnnotated), (returnsResponse || streams) ? implemented.TypeArguments[1] : unit, type, returnsVoid, streams));
                    }
                }
            }
        }

        requests = requests.OrderBy(static item => Name(item.Message), StringComparer.Ordinal).ToList();
        notifications = notifications.OrderBy(static item => item.Handler.ContainingAssembly.Identity.Name, StringComparer.Ordinal)
            .ThenBy(static item => Name(item.Handler), StringComparer.Ordinal).ToList();
        return (requests, notifications);
    }
}
