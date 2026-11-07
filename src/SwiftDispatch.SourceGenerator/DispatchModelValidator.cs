using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using static SwiftDispatch.SourceGenerator.SymbolHelpers;
using static SwiftDispatch.SourceGenerator.GeneratorDiagnostics;

namespace SwiftDispatch.SourceGenerator;

internal static class DispatchModelValidator
{
    internal static Dictionary<INamedTypeSymbol, IMethodSymbol> Validate(
        Compilation compilation, ISymbol accessContext, List<Request> requests, List<Observer> notifications,
        List<INamedTypeSymbol> configuredBehaviors, List<INamedTypeSymbol> configuredStreamBehaviors,
        INamedTypeSymbol pipeline, INamedTypeSymbol streamPipeline, INamedTypeSymbol? notificationStrategy,
        Action<DiagnosticDescriptor, ISymbol, string> reportError)
    {
        var seen = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        foreach (Request request in requests)
        {
            if (!seen.Add(request.Message))
            {
                reportError(Duplicate, request.Handler, $"Request '{Name(request.Message)}' has multiple handler registrations. Exactly one handler and response shape is supported per concrete request.");
            }
        }

        foreach (var configurationGroup in new[] { (Behaviors: configuredBehaviors, Contract: pipeline, Streams: false), (Behaviors: configuredStreamBehaviors, Contract: streamPipeline, Streams: true) })
        {
            foreach (INamedTypeSymbol behavior in configurationGroup.Behaviors)
            {
                INamedTypeSymbol behaviorContract = configurationGroup.Contract;
                INamedTypeSymbol definition = behavior.IsUnboundGenericType ? behavior.OriginalDefinition : behavior;
                if (definition.TypeKind != TypeKind.Class || definition.IsAbstract || definition.IsStatic
                    || (IsOpen(definition) && (!behavior.IsUnboundGenericType || definition.Arity != 2 || definition.ContainingType != null))
                    || !definition.AllInterfaces.Any(item => SymbolEqualityComparer.Default.Equals(item.OriginalDefinition, behaviorContract))
                    || behavior.IsUnboundGenericType && !definition.AllInterfaces.Any(item => SymbolEqualityComparer.Default.Equals(item.OriginalDefinition, behaviorContract)
                        && SymbolEqualityComparer.Default.Equals(item.TypeArguments[0], definition.TypeParameters[0])
                        && SymbolEqualityComparer.Default.Equals(item.TypeArguments[1], definition.TypeParameters[1])))
                {
                    reportError(Behavior, behavior, $"Behavior '{Name(behavior)}' must be a concrete closed class or an open two-parameter class implementing {behaviorContract.Name}<TRequest,TResponse>.");
                    continue;
                }

                foreach (Request request in requests.Where(item => item.Streams == configurationGroup.Streams))
                {
                    INamedTypeSymbol closed = definition;
                    if (behavior.IsUnboundGenericType)
                    {
                        ITypeSymbol[] arguments = { request.Message, request.Response };
                        if (!SatisfiesConstraints(compilation, definition, arguments))
                        {
                            continue;
                        }
                        closed = definition.Construct(arguments);
                    }

                    if (closed.AllInterfaces.Any(item => SymbolEqualityComparer.Default.Equals(item.OriginalDefinition, behaviorContract)
                        && SymbolEqualityComparer.Default.Equals(item.TypeArguments[0], request.Message)
                        && SymbolEqualityComparer.Default.Equals(item.TypeArguments[1], request.Response)))
                    {
                        request.Behaviors.Add(closed);
                    }
                    else if (behavior.IsUnboundGenericType)
                    {
                        reportError(Behavior, behavior, $"Open behavior '{Name(behavior)}' must implement {behaviorContract.Name}<TRequest,TResponse> using its type parameters in that order.");
                    }
                }
            }
        }

        var components = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (Request request in requests)
        {
            components.Add(request.Handler);
            foreach (INamedTypeSymbol behavior in request.Behaviors)
            {
                components.Add(behavior);
            }
        }
        foreach (Observer observer in notifications)
        {
            components.Add(observer.Handler);
        }

        if (notificationStrategy != null) components.Add(notificationStrategy);

        var constructors = new Dictionary<INamedTypeSymbol, IMethodSymbol>(SymbolEqualityComparer.Default);
        foreach (INamedTypeSymbol component in components)
        {
            bool referenced = !SymbolEqualityComparer.Default.Equals(component.ContainingAssembly, compilation.Assembly);
            if (!compilation.IsSymbolAccessibleWithin(component, accessContext)
                || (referenced && !PubliclyAccessible(component)))
            {
                reportError(GeneratorDiagnostics.Accessibility, component, $"Component '{Name(component)}' must be accessible from the host configuration; components in referenced assemblies must be public.");
                continue;
            }

            IMethodSymbol[] accessible = component.InstanceConstructors
                .Where(item => compilation.IsSymbolAccessibleWithin(item, accessContext)).ToArray();
            if (accessible.Length != 1 || accessible[0].Parameters.Any(static parameter => parameter.RefKind != RefKind.None
                || parameter.Type.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer || parameter.Type.IsRefLikeType)
                || HasRequiredMembers(component))
            {
                reportError(Constructor, component, $"Component '{Name(component)}' needs exactly one accessible constructor, ordinary DI parameters, and no uninitialized required members.");
                continue;
            }
            constructors.Add(component, accessible[0]);
        }

        foreach (ITypeSymbol message in requests.Select(static item => item.Message)
            .Concat(requests.Select(static item => item.Response)).Concat(notifications.Select(static item => item.Message)))
        {
            if (!compilation.IsSymbolAccessibleWithin(message, accessContext))
            {
                reportError(GeneratorDiagnostics.Accessibility, accessContext, $"Message/response '{Name(message)}' is inaccessible from the host configuration.");
            }
        }

        return constructors;
    }
}
