using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static SwiftDispatch.SourceGenerator.SymbolHelpers;

namespace SwiftDispatch.SourceGenerator;

internal static class RegistrationParser
{
    internal static bool IsServiceRegistration(Compilation compilation, InvocationExpressionSyntax call)
    {
        var member = (MemberAccessExpressionSyntax)call.Expression;
        ITypeSymbol? receiver = compilation.GetSemanticModel(call.SyntaxTree).GetTypeInfo(member.Expression).Type;
        return receiver?.ToDisplayString() == "Microsoft.Extensions.DependencyInjection.IServiceCollection"
            || receiver?.AllInterfaces.Any(type => type.ToDisplayString() == "Microsoft.Extensions.DependencyInjection.IServiceCollection") == true;
    }

    internal static bool ReadFluentConfiguration(Compilation compilation, InvocationExpressionSyntax call,
        HashSet<IAssemblySymbol> assemblies, List<INamedTypeSymbol> behaviors,
        List<INamedTypeSymbol> streamBehaviors, out INamedTypeSymbol? notificationStrategy, out string? error)
    {
        notificationStrategy = null;
        error = "Use a direct lambda containing registration calls on its options parameter. Assemblies must use Assembly.GetExecutingAssembly() or typeof(T).Assembly, and behaviors must use typeof(T). Variables, helper calls and conditional configuration are unsupported.";
        ExpressionSyntax expression = call.ArgumentList.Arguments[0].Expression;
        string parameter;
        CSharpSyntaxNode body;
        if (expression is SimpleLambdaExpressionSyntax simple)
        {
            parameter = simple.Parameter.Identifier.ValueText;
            body = simple.Body;
        }
        else if (expression is ParenthesizedLambdaExpressionSyntax lambda && lambda.ParameterList.Parameters.Count == 1)
        {
            parameter = lambda.ParameterList.Parameters[0].Identifier.ValueText;
            body = lambda.Body;
        }
        else return false;

        var expressions = new List<ExpressionSyntax>();
        if (body is ExpressionSyntax expressionBody) expressions.Add(expressionBody);
        else if (body is BlockSyntax block)
        {
            foreach (StatementSyntax statement in block.Statements)
            {
                if (statement is not ExpressionStatementSyntax expressionStatement) return false;
                expressions.Add(expressionStatement.Expression);
            }
        }
        else return false;

        var calls = new List<InvocationExpressionSyntax>();
        foreach (ExpressionSyntax statement in expressions)
        {
            var steps = new Stack<InvocationExpressionSyntax>();
            ExpressionSyntax receiver = statement;
            while (receiver is InvocationExpressionSyntax step && step.Expression is MemberAccessExpressionSyntax member)
            {
                steps.Push(step);
                receiver = member.Expression;
            }
            if (receiver is not IdentifierNameSyntax identifier || identifier.Identifier.ValueText != parameter || steps.Count == 0)
                return false;
            calls.AddRange(steps);
        }

        SemanticModel model = compilation.GetSemanticModel(call.SyntaxTree);
        foreach (InvocationExpressionSyntax step in calls)
        {
            var member = (MemberAccessExpressionSyntax)step.Expression;
            if (member.Name is GenericNameSyntax { Identifier.ValueText: "AddHandlersFromAssemblyContaining", TypeArgumentList.Arguments.Count: 1 } generic
                && step.ArgumentList.Arguments.Count == 0)
            {
                if (model.GetTypeInfo(generic.TypeArgumentList.Arguments[0]).Type is not INamedTypeSymbol marker
                    || marker.TypeKind == TypeKind.Error || marker.IsStatic || IsOpen(marker)) return false;
                assemblies.Add(marker.ContainingAssembly);
            }
            else if (member.Name is GenericNameSyntax { Identifier.ValueText: "UseNotificationExecutionStrategy", TypeArgumentList.Arguments.Count: 1 } strategyName
                && step.ArgumentList.Arguments.Count == 0)
            {
                if (notificationStrategy != null
                    || model.GetTypeInfo(strategyName.TypeArgumentList.Arguments[0]).Type is not INamedTypeSymbol strategy
                    || strategy.TypeKind != TypeKind.Class || strategy.IsAbstract || strategy.IsStatic || IsOpen(strategy)
                    || !strategy.AllInterfaces.Any(item => SymbolEqualityComparer.Default.Equals(item,
                        compilation.GetTypeByMetadataName("SwiftDispatch.INotificationExecutionStrategy"))))
                {
                    error = "Select one concrete, closed class implementing INotificationExecutionStrategy with UseNotificationExecutionStrategy<TStrategy>().";
                    return false;
                }
                notificationStrategy = strategy;
            }
            else if (member.Name.Identifier.ValueText == "RegisterServicesFromAssembly"
                && step.ArgumentList.Arguments.Count == 1)
            {
                ExpressionSyntax assemblyExpression = step.ArgumentList.Arguments[0].Expression;
                if (assemblyExpression is InvocationExpressionSyntax assemblyCall
                    && assemblyCall.ArgumentList.Arguments.Count == 0
                    && model.GetSymbolInfo(assemblyCall).Symbol is IMethodSymbol assemblyMethod
                    && assemblyMethod.Name == "GetExecutingAssembly"
                    && assemblyMethod.ContainingType.ToDisplayString() == "System.Reflection.Assembly")
                {
                    assemblies.Add(compilation.Assembly);
                }
                else if (assemblyExpression is MemberAccessExpressionSyntax { Expression: TypeOfExpressionSyntax typeOfAssembly, Name.Identifier.ValueText: "Assembly" }
                    && model.GetTypeInfo(typeOfAssembly.Type).Type is INamedTypeSymbol assemblyType
                    && assemblyType.TypeKind != TypeKind.Error)
                {
                    assemblies.Add(assemblyType.ContainingAssembly);
                }
                else return false;
            }
            else if (member.Name is IdentifierNameSyntax name
                && (name.Identifier.ValueText == "AddPipelineBehavior" || name.Identifier.ValueText == "AddOpenBehavior" || name.Identifier.ValueText == "AddStreamPipelineBehavior")
                && step.ArgumentList.Arguments.Count == 1
                && step.ArgumentList.Arguments[0].Expression is TypeOfExpressionSyntax typeOf
                && model.GetTypeInfo(typeOf.Type).Type is INamedTypeSymbol behavior && behavior.TypeKind != TypeKind.Error)
            {
                (name.Identifier.ValueText == "AddStreamPipelineBehavior" ? streamBehaviors : behaviors).Add(behavior);
            }
            else return false;
        }
        error = null;
        return true;
    }
}
