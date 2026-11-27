using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using static TrueMediator.SourceGenerator.SymbolHelpers;
using static TrueMediator.SourceGenerator.GeneratorDiagnostics;

namespace TrueMediator.SourceGenerator;

internal static class TransactionConfiguration
{
    internal static Dictionary<ITypeSymbol, INamedTypeSymbol> Analyze(Compilation compilation,
        IEnumerable<IAssemblySymbol> assemblies, INamedTypeSymbol? defaultContext, bool requireContext,
        Location fallback, System.Threading.CancellationToken cancellationToken,
        Action<DiagnosticDescriptor, Location, string> reportError)
    {
        var contexts = new Dictionary<ITypeSymbol, INamedTypeSymbol>(SymbolEqualityComparer.Default);
        INamedTypeSymbol? attributeType = compilation.GetTypeByMetadataName("TrueMediator.EntityFrameworkCore.TransactionalAttribute");
        INamedTypeSymbol? dbContext = compilation.GetTypeByMetadataName("Microsoft.EntityFrameworkCore.DbContext");
        INamedTypeSymbol? query = compilation.GetTypeByMetadataName("TrueMediator.Contracts.IQuery`1");
        INamedTypeSymbol? command = compilation.GetTypeByMetadataName("TrueMediator.Contracts.ICommandBase");
        INamedTypeSymbol? request = compilation.GetTypeByMetadataName("TrueMediator.Contracts.IRequest");
        INamedTypeSymbol? responseRequest = compilation.GetTypeByMetadataName("TrueMediator.Contracts.IRequest`1");
        INamedTypeSymbol? stream = compilation.GetTypeByMetadataName("TrueMediator.Contracts.IStreamRequest`1");
        INamedTypeSymbol? notification = compilation.GetTypeByMetadataName("TrueMediator.Contracts.INotification");
        bool Is(INamedTypeSymbol symbol, INamedTypeSymbol? contract) => contract != null
            && SymbolEqualityComparer.Default.Equals(symbol.OriginalDefinition, contract);
        bool ValidContext(INamedTypeSymbol? type)
        {
            if (type == null || type.TypeKind != TypeKind.Class || type.IsAbstract || type.IsStatic || IsOpen(type)
                || !compilation.IsSymbolAccessibleWithin(type, compilation.Assembly)) return false;
            for (INamedTypeSymbol? current = type; current != null; current = current.BaseType)
                if (Is(current, dbContext)) return true;
            return false;
        }

        if (defaultContext != null && !ValidContext(defaultContext))
        {
            reportError(TransactionContext, fallback, "The default transaction context must be an accessible, concrete, closed DbContext type.");
        }

        foreach (IAssemblySymbol assembly in assemblies.OrderBy(item => item.Identity.ToString(), StringComparer.Ordinal))
        foreach (INamedTypeSymbol type in Types(assembly.GlobalNamespace).OrderBy(Name, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool isQuery = type.AllInterfaces.Any(item => Is(item, query));
            bool isCommand = type.AllInterfaces.Any(item => Is(item, command));
            bool concrete = type.TypeKind is TypeKind.Class or TypeKind.Struct && !type.IsAbstract && !type.IsStatic && !IsOpen(type);
            Location location = type.Locations.FirstOrDefault(item => item.IsInSource) ?? fallback;
            if (concrete && isQuery && isCommand)
                reportError(RequestIntent, location, $"Request '{Name(type)}' cannot implement both command and query contracts.");

            AttributeData? attribute = attributeType == null ? null : type.GetAttributes()
                .FirstOrDefault(item => SymbolEqualityComparer.Default.Equals(item.AttributeClass, attributeType));
            if (attribute == null) continue;
            location = attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? location;
            if (!concrete || isQuery || type.AllInterfaces.Any(item => Is(item, stream) || Is(item, notification))
                || !type.AllInterfaces.Any(item => Is(item, request) || Is(item, responseRequest)))
            {
                reportError(TransactionTarget, location, $"[Transactional] on '{Name(type)}' requires a concrete, closed response or void request. Queries, streams, and notifications cannot be transactional.");
                continue;
            }

            bool explicitContext = attribute.ConstructorArguments.Length == 1;
            INamedTypeSymbol? selected = explicitContext ? attribute.ConstructorArguments[0].Value as INamedTypeSymbol : defaultContext;
            if (selected == null && !explicitContext && !requireContext) continue;
            if (!ValidContext(selected))
            {
                reportError(TransactionContext, location, selected == null && !explicitContext
                    ? $"[Transactional] on '{Name(type)}' needs a default UseEntityFrameworkTransactions<TContext>() or an explicit [Transactional(typeof(TContext))]."
                    : $"Transaction context for '{Name(type)}' must be an accessible, concrete, closed DbContext type.");
                continue;
            }
            contexts.Add(type, selected!);
        }
        return contexts;
    }
}
