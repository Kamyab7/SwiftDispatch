using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static SwiftDispatch.SourceGenerator.GeneratorDiagnostics;
using static SwiftDispatch.SourceGenerator.RegistrationParser;

namespace SwiftDispatch.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class SwiftDispatchGenerator : IIncrementalGenerator
{
    private const string Contracts = "SwiftDispatch.Contracts.";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var registrations = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "AddSwiftDispatch" },
                ArgumentList.Arguments.Count: 1
            },
            static (syntax, _) => (InvocationExpressionSyntax)syntax.Node).Collect();

        context.RegisterSourceOutput(registrations.Combine(context.CompilationProvider),
            static (output, input) => Generate(output, input.Right, input.Left));
    }

    private static void Generate(SourceProductionContext output, Compilation compilation, ImmutableArray<InvocationExpressionSyntax> registrations)
    {
        var fluentCalls = registrations.Where(call => IsServiceRegistration(compilation, call)).ToArray();
        if (fluentCalls.Length == 0) return;

        bool failed = false;
        ISymbol accessContext = compilation.Assembly;
        Location configurationLocation = fluentCalls[0].GetLocation();
        void Error(DiagnosticDescriptor rule, ISymbol symbol, string message)
        {
            failed = true;
            Location location = symbol is IAssemblySymbol ? configurationLocation
                : symbol.Locations.FirstOrDefault(static item => item.IsInSource) ?? configurationLocation;
            output.ReportDiagnostic(Diagnostic.Create(rule, location, message));
        }

        if (fluentCalls.Length > 1)
        {
            Error(Configuration, accessContext, "Use one fluent AddSwiftDispatch registration per assembly. To register repeatedly, wrap that call in a helper method.");
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
            Error(Configuration, accessContext, "Reference SwiftDispatch and SwiftDispatch.Contracts before generating dispatch.");
            return;
        }

        var assemblies = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default) { compilation.Assembly };
        var configuredBehaviors = new List<INamedTypeSymbol>();
        var configuredStreamBehaviors = new List<INamedTypeSymbol>();
        if (!ReadFluentConfiguration(compilation, fluentCalls[0], assemblies,
            configuredBehaviors, configuredStreamBehaviors, out INamedTypeSymbol? notificationStrategy, out string? error))
        {
            Error(Configuration, accessContext, error!);
            return;
        }
        var (requests, notifications) = HandlerDiscovery.Discover(assemblies, responseHandler,
            streamHandler, voidHandler, notificationHandler, unit, output.CancellationToken, Error);
        var constructors = DispatchModelValidator.Validate(compilation, accessContext, requests, notifications,
            configuredBehaviors, configuredStreamBehaviors, pipeline, streamPipeline, notificationStrategy, Error);

        if (failed)
        {
            return;
        }

        output.AddSource("SwiftDispatch.Registration.g.cs", SourceText.From(SourceEmitter.Emit(requests, notifications, constructors, notificationStrategy), Encoding.UTF8));
    }
}
