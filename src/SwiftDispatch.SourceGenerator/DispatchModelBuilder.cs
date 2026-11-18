using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using static SwiftDispatch.SourceGenerator.SymbolHelpers;

namespace SwiftDispatch.SourceGenerator;

/// <summary>Freezes validated analysis into the representation consumed by emitters.</summary>
internal static class DispatchModelBuilder
{
    internal static DispatchModel Build(IReadOnlyList<Request> requests, IReadOnlyList<Observer> notifications,
        IReadOnlyDictionary<INamedTypeSymbol, IMethodSymbol> constructors, INamedTypeSymbol? notificationStrategy, bool enableDispatchCatalog)
    {
        return new DispatchModel(
            requests.Select(request => new RequestDispatchModel(Name(request.Message), Name(request.Response),
                Resolve(request.Handler), request.ReturnsVoid, request.Streams,
                request.Behaviors.Select(Resolve).ToImmutableArray())).ToImmutableArray(),
            notifications.GroupBy<Observer, ITypeSymbol>(observer => observer.Message, SymbolEqualityComparer.Default)
                .OrderBy(group => Name(group.Key), StringComparer.Ordinal)
                .Select(group => new NotificationDispatchModel(Name(group.Key),
                    group.Select(observer => Resolve(observer.Handler)).ToImmutableArray())).ToImmutableArray(),
            constructors.OrderBy(component => Name(component.Key), StringComparer.Ordinal)
                .Select(component => new ComponentRegistrationModel(Name(component.Key),
                    component.Value.Parameters.Select(parameter => Resolve(parameter.Type)).ToImmutableArray())).ToImmutableArray(),
            notificationStrategy == null ? null : Resolve(notificationStrategy),
            enableDispatchCatalog ? BuildCatalog(requests, notifications, notificationStrategy) : default);
    }

    private static ImmutableArray<CatalogEntryModel> BuildCatalog(IReadOnlyList<Request> requests,
        IReadOnlyList<Observer> notifications, INamedTypeSymbol? notificationStrategy)
    {
        var entries = requests.Select(request => new CatalogEntryModel(Identity(request.Message),
            request.ReturnsVoid ? null : TypeName(request.Response), request.Streams ? "Stream" : "Request",
            request.Behaviors.Select(type => Identity(type, request.Streams ? "IStreamPipelineBehavior" : "IPipelineBehavior", request.Message, request.Response)).ToImmutableArray(),
            ImmutableArray.Create(Identity(request.Handler, request.Streams ? "IStreamRequestHandler" : "IRequestHandler", request.Message, request.ReturnsVoid ? null : request.Response)), null));
        var observers = notifications.GroupBy<Observer, ITypeSymbol>(observer => observer.Message, SymbolEqualityComparer.Default)
            .Select(group => new CatalogEntryModel(Identity(group.Key), null, "Notification",
                ImmutableArray<CatalogTypeModel>.Empty,
                group.Select(observer => Identity(observer.Handler, "INotificationHandler", observer.Message)).ToImmutableArray(),
                notificationStrategy == null
                    ? new CatalogTypeModel("global::SwiftDispatch.SequentialNotificationExecutionStrategy", null)
                    : Identity(notificationStrategy)));
        return entries.Concat(observers).OrderBy(entry => entry.Message.TypeName, StringComparer.Ordinal)
            .ThenBy(entry => entry.Kind == "Request" ? 0 : entry.Kind == "Stream" ? 1 : 2).ToImmutableArray();
    }

    private static string TypeName(ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions
            & ~SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier));

    private static CatalogTypeModel Identity(ITypeSymbol type, string? contractName = null, ITypeSymbol? message = null, ITypeSymbol? response = null)
    {
        ISymbol symbol = type;
        if (contractName != null && type is INamedTypeSymbol named)
        {
            // Resolve the actual contract implementation, including inherited and explicit methods.
            foreach (INamedTypeSymbol contract in named.AllInterfaces)
            {
                if (contract.ContainingNamespace.ToDisplayString() != "SwiftDispatch.Contracts"
                    || contract.Name != contractName || contract.TypeArguments.Length != (response == null ? 1 : 2)
                    || !SymbolEqualityComparer.Default.Equals(contract.TypeArguments[0], message)
                    || response != null && !SymbolEqualityComparer.Default.Equals(contract.TypeArguments[1], response)) continue;
                foreach (IMethodSymbol method in contract.GetMembers("Handle").OfType<IMethodSymbol>())
                {
                    if (named.FindImplementationForInterfaceMember(method) is IMethodSymbol implementation)
                    {
                        symbol = implementation;
                        break;
                    }
                }
                if (!SymbolEqualityComparer.Default.Equals(symbol, type)) break;
            }
        }
        Location? location = symbol.Locations.FirstOrDefault(item => item.IsInSource)
            ?? type.Locations.FirstOrDefault(item => item.IsInSource);
        string? hint = null;
        if (location != null)
        {
            FileLinePositionSpan span = location.GetLineSpan();
            hint = (string.IsNullOrEmpty(span.Path) ? "<source>" : span.Path) + ":" + (span.StartLinePosition.Line + 1);
        }
        return new CatalogTypeModel(TypeName(type), hint);
    }

    private static string Resolve(ITypeSymbol type)
    {
        const string resolver = "global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService";
        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return "(" + Name(type) + ")" + resolver + "(sp, typeof(" + Name(type) + "))";
        }
        return resolver + "<" + Name(type.WithNullableAnnotation(NullableAnnotation.NotAnnotated)) + ">(sp)";
    }
}
