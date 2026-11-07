using Microsoft.CodeAnalysis;


namespace SwiftDispatch.SourceGenerator;

internal static class GeneratorDiagnostics
{
    internal static readonly DiagnosticDescriptor Configuration = Rule("SD001", "Invalid configuration");
    internal static readonly DiagnosticDescriptor Accessibility = Rule("SD002", "Inaccessible component");
    internal static readonly DiagnosticDescriptor Constructor = Rule("SD003", "Invalid constructor");
    internal static readonly DiagnosticDescriptor Duplicate = Rule("SD004", "Duplicate request handlers");
    internal static readonly DiagnosticDescriptor OpenHandler = Rule("SD005", "Open generic handler");
    internal static readonly DiagnosticDescriptor Behavior = Rule("SD006", "Invalid pipeline behavior");

    private static DiagnosticDescriptor Rule(string id, string title) => new(
        id, title, "{0}", "SwiftDispatch", DiagnosticSeverity.Error, isEnabledByDefault: true);
}
