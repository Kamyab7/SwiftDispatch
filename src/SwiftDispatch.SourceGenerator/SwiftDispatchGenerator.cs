using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SwiftDispatch.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class SwiftDispatchGenerator : IIncrementalGenerator
{
    private const string Contracts = "SwiftDispatch.Contracts.";
    private static readonly DiagnosticDescriptor Configuration = Rule("SD001", "Invalid configuration");
    private static readonly DiagnosticDescriptor Accessibility = Rule("SD002", "Inaccessible component");
    private static readonly DiagnosticDescriptor Constructor = Rule("SD003", "Invalid constructor");
    private static readonly DiagnosticDescriptor Duplicate = Rule("SD004", "Duplicate request handlers");
    private static readonly DiagnosticDescriptor OpenHandler = Rule("SD005", "Open generic handler");
    private static readonly DiagnosticDescriptor Behavior = Rule("SD006", "Invalid pipeline behavior");

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<ImmutableArray<INamedTypeSymbol>> configurations = context.SyntaxProvider
            .ForAttributeWithMetadataName(Contracts + "GenerateSwiftDispatchAttribute",
                static (node, _) => node is ClassDeclarationSyntax,
                static (attribute, _) => (INamedTypeSymbol)attribute.TargetSymbol)
            .Collect();

        context.RegisterSourceOutput(configurations.Combine(context.CompilationProvider),
            static (output, input) => Generate(output, input.Right, input.Left));
    }

    private static DiagnosticDescriptor Rule(string id, string title) => new(
        id, title, "{0}", "SwiftDispatch", DiagnosticSeverity.Error, isEnabledByDefault: true);

    private static string Name(ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions
            | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier));

    private static void Generate(SourceProductionContext output, Compilation compilation, ImmutableArray<INamedTypeSymbol> configurations)
    {
        if (configurations.Length == 0)
        {
            return;
        }

        bool failed = false;
        INamedTypeSymbol configuration = configurations[0];
        void Error(DiagnosticDescriptor rule, ISymbol symbol, string message)
        {
            failed = true;
            Location location = symbol.Locations.FirstOrDefault(static item => item.IsInSource)
                ?? configuration.Locations.FirstOrDefault() ?? Location.None;
            output.ReportDiagnostic(Diagnostic.Create(rule, location, message));
        }

        if (configurations.Length != 1 || !configuration.IsStatic || configuration.Arity != 0
            || configuration.ContainingType != null || configuration.IsFileLocal
            || configuration.GetMembers("AddSwiftDispatch").Length != 0
            || configuration.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax(output.CancellationToken)
                is not ClassDeclarationSyntax declaration || !declaration.Modifiers.Any(SyntaxKind.PartialKeyword)))
        {
            Error(Configuration, configuration, "Use exactly one top-level, non-generic static partial configuration class per host, with no existing AddSwiftDispatch member.");
            return;
        }

        INamedTypeSymbol? responseHandler = compilation.GetTypeByMetadataName(Contracts + "IRequestHandler`2");
        INamedTypeSymbol? streamHandler = compilation.GetTypeByMetadataName(Contracts + "IStreamRequestHandler`2");
        INamedTypeSymbol? streamPipeline = compilation.GetTypeByMetadataName(Contracts + "IStreamPipelineBehavior`2");
        INamedTypeSymbol? voidHandler = compilation.GetTypeByMetadataName(Contracts + "IRequestHandler`1");
        INamedTypeSymbol? notificationHandler = compilation.GetTypeByMetadataName(Contracts + "INotificationHandler`1");
        INamedTypeSymbol? pipeline = compilation.GetTypeByMetadataName(Contracts + "IPipelineBehavior`2");
        INamedTypeSymbol? unit = compilation.GetTypeByMetadataName(Contracts + "Unit");
        if (streamHandler == null || streamPipeline == null || responseHandler == null || voidHandler == null || notificationHandler == null || pipeline == null || unit == null)
        {
            Error(Configuration, configuration, "Reference SwiftDispatch and SwiftDispatch.Contracts before generating dispatch.");
            return;
        }

        AttributeData attribute = configuration.GetAttributes().First(item => item.AttributeClass?.ToDisplayString() == Contracts + "GenerateSwiftDispatchAttribute");
        var assemblies = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default) { compilation.Assembly };
        var configuredBehaviors = new List<INamedTypeSymbol>();
        var configuredStreamBehaviors = new List<INamedTypeSymbol>();
        foreach (KeyValuePair<string, TypedConstant> argument in attribute.NamedArguments)
        {
            if (argument.Value.IsNull)
            {
                Error(Configuration, configuration, argument.Key + " must be a non-null array of types.");
                continue;
            }

            foreach (TypedConstant value in argument.Value.Values)
            {
                if (value.Value is not INamedTypeSymbol type)
                {
                    Error(Configuration, configuration, argument.Key + " must contain named types.");
                    continue;
                }

                if (argument.Key == "HandlerAssemblyMarkers")
                {
                    assemblies.Add(type.ContainingAssembly);
                }
                else if (argument.Key == "StreamPipelineBehaviors")
                {
                    configuredStreamBehaviors.Add(type);
                }
                else if (argument.Key == "PipelineBehaviors")
                {
                    configuredBehaviors.Add(type);
                }
            }
        }

        var requests = new List<Request>();
        var notifications = new List<Observer>();
        foreach (IAssemblySymbol assembly in assemblies.OrderBy(static item => item.Identity.ToString(), StringComparer.Ordinal))
        {
            foreach (INamedTypeSymbol type in Types(assembly.GlobalNamespace).OrderBy(Name, StringComparer.Ordinal))
            {
                output.CancellationToken.ThrowIfCancellationRequested();
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
                        Error(OpenHandler, type, $"Handler '{Name(type)}' is open generic. Supply a concrete, closed handler class.");
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
        var seen = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        foreach (Request request in requests)
        {
            if (!seen.Add(request.Message))
            {
                Error(Duplicate, request.Handler, $"Request '{Name(request.Message)}' has multiple handler registrations. Exactly one handler and response shape is supported per concrete request.");
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
                    Error(Behavior, behavior, $"Behavior '{Name(behavior)}' must be a concrete closed class or an open two-parameter class implementing {behaviorContract.Name}<TRequest,TResponse>.");
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
                        Error(Behavior, behavior, $"Open behavior '{Name(behavior)}' must implement {behaviorContract.Name}<TRequest,TResponse> using its type parameters in that order.");
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

        var constructors = new Dictionary<INamedTypeSymbol, IMethodSymbol>(SymbolEqualityComparer.Default);
        foreach (INamedTypeSymbol component in components)
        {
            bool referenced = !SymbolEqualityComparer.Default.Equals(component.ContainingAssembly, compilation.Assembly);
            if (!compilation.IsSymbolAccessibleWithin(component, configuration)
                || (referenced && !PubliclyAccessible(component)))
            {
                Error(Accessibility, component, $"Component '{Name(component)}' must be accessible from the host configuration; components in referenced assemblies must be public.");
                continue;
            }

            IMethodSymbol[] accessible = component.InstanceConstructors
                .Where(item => compilation.IsSymbolAccessibleWithin(item, configuration)).ToArray();
            if (accessible.Length != 1 || accessible[0].Parameters.Any(static parameter => parameter.RefKind != RefKind.None
                || parameter.Type.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer || parameter.Type.IsRefLikeType)
                || HasRequiredMembers(component))
            {
                Error(Constructor, component, $"Component '{Name(component)}' needs exactly one accessible constructor, ordinary DI parameters, and no uninitialized required members.");
                continue;
            }
            constructors.Add(component, accessible[0]);
        }

        foreach (ITypeSymbol message in requests.Select(static item => item.Message)
            .Concat(requests.Select(static item => item.Response)).Concat(notifications.Select(static item => item.Message)))
        {
            if (!compilation.IsSymbolAccessibleWithin(message, configuration))
            {
                Error(Accessibility, configuration, $"Message/response '{Name(message)}' is inaccessible from the host configuration.");
            }
        }

        if (failed)
        {
            return;
        }

        output.AddSource("SwiftDispatch.Registration.g.cs", SourceText.From(Emit(configuration, requests, notifications, constructors), Encoding.UTF8));
    }

    private static bool PubliclyAccessible(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type; current != null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility != Microsoft.CodeAnalysis.Accessibility.Public)
            {
                return false;
            }
        }
        return true;
    }

    private static bool HasRequiredMembers(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type; current != null; current = current.BaseType)
        {
            if (current.GetMembers().Any(static member => member is IPropertySymbol { IsRequired: true } or IFieldSymbol { IsRequired: true }))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsOpen(INamedTypeSymbol type) => type.IsUnboundGenericType
        || type.TypeArguments.Any(static argument => argument.TypeKind == TypeKind.TypeParameter
            || argument is INamedTypeSymbol named && IsOpen(named))
        || type.ContainingType != null && IsOpen(type.ContainingType);

    private static IEnumerable<INamedTypeSymbol> Types(INamespaceSymbol root)
    {
        foreach (INamespaceSymbol child in root.GetNamespaceMembers())
        {
            foreach (INamedTypeSymbol type in Types(child))
            {
                yield return type;
            }
        }
        foreach (INamedTypeSymbol type in root.GetTypeMembers())
        {
            yield return type;
            foreach (INamedTypeSymbol nested in NestedTypes(type))
            {
                yield return nested;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> NestedTypes(INamedTypeSymbol parent)
    {
        foreach (INamedTypeSymbol type in parent.GetTypeMembers())
        {
            yield return type;
            foreach (INamedTypeSymbol nested in NestedTypes(type))
            {
                yield return nested;
            }
        }
    }

    private static bool SatisfiesConstraints(Compilation compilation, INamedTypeSymbol definition, ITypeSymbol[] arguments)
    {
        var substitutions = new Dictionary<ITypeParameterSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
        for (int index = 0; index < arguments.Length; index++)
        {
            substitutions.Add(definition.TypeParameters[index], arguments[index]);
        }
        for (int index = 0; index < arguments.Length; index++)
        {
            ITypeParameterSymbol parameter = definition.TypeParameters[index];
            ITypeSymbol argument = arguments[index];
            if (parameter.HasReferenceTypeConstraint && (!argument.IsReferenceType
                    || parameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.NotAnnotated && argument.NullableAnnotation == NullableAnnotation.Annotated)
                || parameter.HasValueTypeConstraint && (!argument.IsValueType || argument.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                || parameter.HasUnmanagedTypeConstraint && !argument.IsUnmanagedType
                || parameter.HasNotNullConstraint && (argument.NullableAnnotation == NullableAnnotation.Annotated || argument.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                || parameter.HasConstructorConstraint && !argument.IsValueType
                    && (argument is not INamedTypeSymbol named || named.IsAbstract
                        || !named.InstanceConstructors.Any(static constructor => constructor.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.Public && constructor.Parameters.Length == 0)))
            {
                return false;
            }
            foreach (ITypeSymbol constraint in parameter.ConstraintTypes)
            {
                ITypeSymbol target = Substitute(compilation, constraint, substitutions);
                if (argument.IsReferenceType && argument.NullableAnnotation == NullableAnnotation.Annotated
                    && target.NullableAnnotation != NullableAnnotation.Annotated)
                {
                    return false;
                }
                Microsoft.CodeAnalysis.Operations.CommonConversion conversion = compilation.ClassifyCommonConversion(argument, target);
                if (!conversion.IsImplicit || !(conversion.IsIdentity || conversion.IsReference
                    || argument.IsValueType && (target.TypeKind == TypeKind.Interface || target.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType)))
                {
                    return false;
                }
            }
        }
        return true;
    }

    private static ITypeSymbol Substitute(Compilation compilation, ITypeSymbol type, Dictionary<ITypeParameterSymbol, ITypeSymbol> substitutions)
    {
        if (type is ITypeParameterSymbol parameter && substitutions.TryGetValue(parameter, out ITypeSymbol? replacement))
        {
            return replacement;
        }
        if (type is IArrayTypeSymbol array)
        {
            return compilation.CreateArrayTypeSymbol(Substitute(compilation, array.ElementType, substitutions), array.Rank);
        }
        if (type is INamedTypeSymbol { IsGenericType: true } named)
        {
            return named.ConstructedFrom.Construct(named.TypeArguments.Select(argument => Substitute(compilation, argument, substitutions)).ToArray());
        }
        return type;
    }

    private static string Emit(INamedTypeSymbol configuration, List<Request> requests, List<Observer> notifications,
        Dictionary<INamedTypeSymbol, IMethodSymbol> constructors)
    {
        var code = new StringBuilder("// <auto-generated/>\n#nullable enable\n");
        if (!configuration.ContainingNamespace.IsGlobalNamespace)
        {
            code.Append("namespace ").Append(configuration.ContainingNamespace.ToDisplayString()).AppendLine(";");
        }
        code.Append(configuration.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.Public ? "public" : "internal")
            .Append(" static partial class @").Append(configuration.Name).AppendLine("\n{");
        code.AppendLine("    public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddSwiftDispatch(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)\n    {");
        code.AppendLine("        global::System.ArgumentNullException.ThrowIfNull(services);");
        foreach (KeyValuePair<INamedTypeSymbol, IMethodSymbol> component in constructors.OrderBy(static pair => Name(pair.Key), StringComparer.Ordinal))
        {
            string type = Name(component.Key);
            code.Append("        global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddTransient<")
                .Append(type).Append(">(services, static sp => new ").Append(type).Append('(');
            code.Append(string.Join(", ", component.Value.Parameters.Select(parameter => Resolve(parameter.Type))));
            code.AppendLine("));");
        }
        code.AppendLine("        global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddSingleton<global::SwiftDispatch.DispatchTable>(services, static _ => CreateSwiftDispatchTable());");
        code.AppendLine("        global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddScoped<global::SwiftDispatch.Mediator>(services, static sp => new global::SwiftDispatch.Mediator(sp, global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::SwiftDispatch.DispatchTable>(sp)));");
        foreach (string contract in new[] { "IMediator", "ISender", "IPublisher" })
        {
            code.Append("        global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddScoped<global::SwiftDispatch.Contracts.")
                .Append(contract).AppendLine(">(services, static sp => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::SwiftDispatch.Mediator>(sp));");
        }
        code.AppendLine("        return services;\n    }");
        var groups = notifications.GroupBy<Observer, ITypeSymbol>(static item => item.Message, SymbolEqualityComparer.Default)
            .OrderBy(static group => Name(group.Key!), StringComparer.Ordinal).ToList();
        code.AppendLine("    private static global::SwiftDispatch.DispatchTable CreateSwiftDispatchTable() => new(\n        new global::System.Collections.Generic.Dictionary<global::System.Type, object>\n        {");
        for (int index = 0; index < requests.Count; index++)
        {
            code.Append("            [typeof(").Append(Name(requests[index].Message)).Append(")] = new __SwiftDispatchRequest")
                .Append(index).AppendLine("(),");
        }
        code.AppendLine("        },\n        new global::System.Collections.Generic.Dictionary<global::System.Type, global::SwiftDispatch.NotificationDispatcher>\n        {");
        for (int index = 0; index < groups.Count; index++)
        {
            code.Append("            [typeof(").Append(Name(groups[index].Key!)).Append(")] = new __SwiftDispatchNotification")
                .Append(index).AppendLine("(),");
        }
        code.AppendLine("        });");

        for (int index = 0; index < requests.Count; index++)
        {
            Request request = requests[index];
            string message = Name(request.Message);
            string response = Name(request.Response);
            string resultType = request.Streams ? "global::System.Collections.Generic.IAsyncEnumerable<" : "global::System.Threading.Tasks.Task<";
            string dispatcherContract = request.Streams ? "IStreamRequestDispatcher" : "IRequestDispatcher";
            string handlerContract = request.Streams ? "IStreamRequestHandler" : "IRequestHandler";
            string behaviorContract = request.Streams ? "IStreamPipelineBehavior" : "IPipelineBehavior";
            string delegateContract = request.Streams ? "StreamHandlerDelegate" : "RequestHandlerDelegate";
            code.Append("    private sealed class __SwiftDispatchRequest").Append(index)
                .Append(" : global::SwiftDispatch.").Append(dispatcherContract).Append("<").Append(response).AppendLine(">\n    {");
            code.Append("        public ").Append(resultType).Append(response)
                .AppendLine("> Invoke(object value, global::System.IServiceProvider sp, global::System.Threading.CancellationToken cancellationToken)\n        {");
            code.Append("            var request = (").Append(message).AppendLine(")value;");
            string handlerInterface = "global::SwiftDispatch.Contracts." + handlerContract + "<" + message + (request.ReturnsVoid ? "" : ", " + response) + ">";
            code.Append("            ").Append(handlerInterface).Append(" handler = ").Append(Resolve(request.Handler)).AppendLine(";");
            if (request.ReturnsVoid)
            {
                code.AppendLine("            async global::System.Threading.Tasks.Task<global::SwiftDispatch.Contracts.Unit> InvokeHandler()\n            {\n                cancellationToken.ThrowIfCancellationRequested();\n                await handler.Handle(request, cancellationToken).ConfigureAwait(false);\n                return global::SwiftDispatch.Contracts.Unit.Value;\n            }");
            }
            else
            {
                code.Append("            ").Append(resultType).Append(response).AppendLine("> InvokeHandler()\n            {\n                cancellationToken.ThrowIfCancellationRequested();\n                return handler.Handle(request, cancellationToken);\n            }");
            }
            code.Append("            global::SwiftDispatch.Contracts.").Append(delegateContract).Append("<").Append(response).AppendLine("> next = InvokeHandler;");
            for (int behaviorIndex = request.Behaviors.Count - 1; behaviorIndex >= 0; behaviorIndex--)
            {
                code.Append("            global::SwiftDispatch.Contracts.").Append(behaviorContract).Append("<").Append(message).Append(", ").Append(response)
                    .Append("> behavior").Append(behaviorIndex).Append(" = ").Append(Resolve(request.Behaviors[behaviorIndex])).AppendLine(";");
                code.Append("            var continuation").Append(behaviorIndex).AppendLine(" = next;");
                code.Append("            next = () =>\n            {\n                cancellationToken.ThrowIfCancellationRequested();\n                return behavior")
                    .Append(behaviorIndex).Append(".Handle(request, continuation").Append(behaviorIndex).AppendLine(", cancellationToken);\n            };");
            }
            code.AppendLine("            return next();\n        }\n    }");
        }

        for (int index = 0; index < groups.Count; index++)
        {
            code.Append("    private sealed class __SwiftDispatchNotification").Append(index).AppendLine(" : global::SwiftDispatch.NotificationDispatcher\n    {");
            code.AppendLine("        public override async global::System.Threading.Tasks.Task Invoke(object value, global::System.IServiceProvider sp, global::System.Threading.CancellationToken cancellationToken)\n        {");
            code.Append("            var notification = (").Append(Name(groups[index].Key!)).AppendLine(")value;");
            int handlerIndex = 0;
            foreach (Observer observer in groups[index])
            {
                code.AppendLine("            cancellationToken.ThrowIfCancellationRequested();");
                code.Append("            global::SwiftDispatch.Contracts.INotificationHandler<").Append(Name(observer.Message))
                    .Append("> handler").Append(handlerIndex).Append(" = ").Append(Resolve(observer.Handler)).AppendLine(";");
                code.Append("            await handler").Append(handlerIndex++).AppendLine(".Handle(notification, cancellationToken).ConfigureAwait(false);");
            }
            code.AppendLine("        }\n    }");
        }
        return code.AppendLine("}").ToString();
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

    private sealed class Request(ITypeSymbol message, ITypeSymbol response, INamedTypeSymbol handler, bool returnsVoid, bool streams)
    {
        public ITypeSymbol Message { get; } = message;
        public ITypeSymbol Response { get; } = response;
        public INamedTypeSymbol Handler { get; } = handler;
        public bool Streams { get; } = streams;
        public bool ReturnsVoid { get; } = returnsVoid;
        public List<INamedTypeSymbol> Behaviors { get; } = new();
    }

    private sealed class Observer(ITypeSymbol message, INamedTypeSymbol handler)
    {
        public ITypeSymbol Message { get; } = message;
        public INamedTypeSymbol Handler { get; } = handler;
    }
}
