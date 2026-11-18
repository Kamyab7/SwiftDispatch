using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SwiftDispatch.SourceGenerator;

internal static class CatalogEmitter
{
    internal static void Emit(StringBuilder code, DispatchModel model)
    {
        code.AppendLine("""
                // CONFIGURED FLOW ONLY: behaviors may short-circuit or retry. Nested dispatches and
                // runtime service overrides can change actual execution. No call-site discovery or runtime trace.
                private sealed class __SwiftDispatchCatalog : global::SwiftDispatch.Diagnostics.IDispatchCatalog
                {
                    public global::System.Collections.Generic.IReadOnlyList<global::SwiftDispatch.Diagnostics.DispatchDescriptor> Entries { get; } =
                        global::System.Array.AsReadOnly(new global::SwiftDispatch.Diagnostics.DispatchDescriptor[]
                        {
            """);
        foreach (CatalogEntryModel entry in model.CatalogEntries)
        {
            Comment(code, $"{entry.Kind}: {entry.Message.TypeName} -> {entry.Response ?? "(no response)"}");
            LocationComment(code, "Message", entry.Message);
            for (int index = 0; index < entry.Behaviors.Length; index++)
                LocationComment(code, $"Behavior {index + 1} (outermost first)", entry.Behaviors[index]);
            foreach (CatalogTypeModel handler in entry.Handlers)
                LocationComment(code, "Handler.Handle", handler);
            if (entry.NotificationStrategy is { } strategy)
            {
                LocationComment(code, "Notification strategy", strategy);
                Comment(code, strategy.TypeName == "global::SwiftDispatch.SequentialNotificationExecutionStrategy"
                    ? "Subscribers execute sequentially in the listed registration order."
                    : "Subscribers are listed in registration order; execution order is determined by the strategy.");
            }
            code.AppendLine($$"""
                                new global::SwiftDispatch.Diagnostics.DispatchDescriptor(
                                    typeof({{entry.Message.TypeName}}), {{(entry.Response == null ? "null" : $"typeof({entry.Response})")}},
                                    global::SwiftDispatch.Diagnostics.DispatchKind.{{entry.Kind}},
                                    {{Types(entry.Behaviors)}}, {{Types(entry.Handlers)}},
                                    {{(entry.NotificationStrategy == null ? "null" : $"typeof({entry.NotificationStrategy.TypeName})")}}),

                """);
        }
        code.AppendLine("""
                        });

                    public global::System.Collections.Generic.IReadOnlyList<global::SwiftDispatch.Diagnostics.DispatchDescriptor> Find(global::System.Type messageType)
                    {
                        if (messageType is null) throw new global::System.ArgumentNullException(nameof(messageType));
                        var matches = new global::System.Collections.Generic.List<global::SwiftDispatch.Diagnostics.DispatchDescriptor>();
                        foreach (var entry in Entries)
                        {
                            if (entry.MessageType == messageType) matches.Add(entry);
                        }
                        return matches.AsReadOnly();
                    }
                }
            """);
    }

    private static string Types(IEnumerable<CatalogTypeModel> types) =>
        "new global::System.Type[] { " + string.Join(", ", types.Select(type => $"typeof({type.TypeName})")) + " }";

    private static void LocationComment(StringBuilder code, string label, CatalogTypeModel type) =>
        Comment(code, $"{label}: {type.TypeName} [{type.SourceLocation ?? "source location unavailable"}]");

    private static void Comment(StringBuilder code, string text)
    {
        // Paths can contain line separators. Never allow a source hint to inject generated code.
        code.Append("                // ");
        foreach (char character in text)
            code.Append(character is '\r' or '\n' or '\u0085' or '\u2028' or '\u2029' ? ' ' : character);
        code.AppendLine();
    }
}
