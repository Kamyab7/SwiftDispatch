using System.Text;

namespace SwiftDispatch.SourceGenerator;

internal static class DispatchTableEmitter
{
    internal static void Emit(StringBuilder code, DispatchModel model)
    {
        code.AppendLine("    private static global::SwiftDispatch.DispatchTable CreateSwiftDispatchTable() => new(\n        new global::System.Collections.Generic.Dictionary<global::System.Type, object>\n        {");
        for (int index = 0; index < model.Requests.Count; index++)
        {
            code.Append("            [typeof(").Append(model.Requests[index].Message).Append(")] = new __SwiftDispatchRequest")
                .Append(index).AppendLine("(),");
        }
        code.AppendLine("        },\n        new global::System.Collections.Generic.Dictionary<global::System.Type, global::SwiftDispatch.NotificationDispatcher>\n        {");
        for (int index = 0; index < model.Notifications.Count; index++)
        {
            code.Append("            [typeof(").Append(model.Notifications[index].Message).Append(")] = new __SwiftDispatchNotification")
                .Append(index).AppendLine("(),");
        }
        code.AppendLine("        });");

    }
}
