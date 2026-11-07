using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;


namespace SwiftDispatch.SourceGenerator;

internal static class SymbolHelpers
{
    internal static string Name(ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions
            | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier));

    internal static bool PubliclyAccessible(INamedTypeSymbol type)
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

    internal static bool HasRequiredMembers(INamedTypeSymbol type)
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

    internal static bool IsOpen(INamedTypeSymbol type) => type.IsUnboundGenericType
        || type.TypeArguments.Any(static argument => argument.TypeKind == TypeKind.TypeParameter
            || argument is INamedTypeSymbol named && IsOpen(named))
        || type.ContainingType != null && IsOpen(type.ContainingType);

    internal static IEnumerable<INamedTypeSymbol> Types(INamespaceSymbol root)
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

    internal static IEnumerable<INamedTypeSymbol> NestedTypes(INamedTypeSymbol parent)
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

    internal static bool SatisfiesConstraints(Compilation compilation, INamedTypeSymbol definition, ITypeSymbol[] arguments)
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

    internal static ITypeSymbol Substitute(Compilation compilation, ITypeSymbol type, Dictionary<ITypeParameterSymbol, ITypeSymbol> substitutions)
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
}
