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
        IReadOnlyDictionary<INamedTypeSymbol, IMethodSymbol> constructors, INamedTypeSymbol? notificationStrategy)
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
            notificationStrategy == null ? null : Resolve(notificationStrategy));
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
