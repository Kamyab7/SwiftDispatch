using Microsoft.CodeAnalysis;


namespace TrueMediator.SourceGenerator;

internal static class GeneratorDiagnostics
{
    internal static readonly DiagnosticDescriptor Configuration = Rule("TM001", "Invalid configuration");
    internal static readonly DiagnosticDescriptor Accessibility = Rule("TM002", "Inaccessible component");
    internal static readonly DiagnosticDescriptor Constructor = Rule("TM003", "Invalid constructor");
    internal static readonly DiagnosticDescriptor Duplicate = Rule("TM004", "Duplicate request handlers");
    internal static readonly DiagnosticDescriptor OpenHandler = Rule("TM005", "Open generic handler");
    internal static readonly DiagnosticDescriptor Behavior = Rule("TM006", "Invalid pipeline behavior");
    internal static readonly DiagnosticDescriptor MissingHandler = Rule("TM007", "Missing request handler");

    internal static readonly DiagnosticDescriptor MissingNotificationHandler = Rule("TM008", "Missing notification handler");

    internal static readonly DiagnosticDescriptor TransactionTarget = Rule("TM009", "Invalid transactional request");
    internal static readonly DiagnosticDescriptor TransactionContext = Rule("TM010", "Invalid transaction context");
    internal static readonly DiagnosticDescriptor RequestIntent = Rule("TM011", "Conflicting command and query contracts");

    private static DiagnosticDescriptor Rule(string id, string title) => new(
        id, title, "{0}", "TrueMediator", DiagnosticSeverity.Error, isEnabledByDefault: true);
}
