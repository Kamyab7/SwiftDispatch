using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using static SwiftDispatch.SourceGenerator.SymbolHelpers;

namespace SwiftDispatch.SourceGenerator;

internal static class DispatchTableEmitter
{
    internal static void Emit(StringBuilder code, List<Request> requests, List<Observer> notifications)
    {
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

    }
}
