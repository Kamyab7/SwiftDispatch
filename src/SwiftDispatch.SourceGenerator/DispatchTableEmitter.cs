using System.Text;

namespace SwiftDispatch.SourceGenerator;

internal static class DispatchTableEmitter
{
    internal static void Emit(StringBuilder code, DispatchModel model)
    {
        code.AppendLine("""
                private static global::SwiftDispatch.DispatchTable CreateSwiftDispatchTable() => new(
                    new global::System.Collections.Generic.Dictionary<global::System.Type, object>
                    {
            """);
        for (int index = 0; index < model.Requests.Count; index++)
        {
            code.AppendLine($$"""
                            [typeof({{model.Requests[index].Message}})] = new __SwiftDispatchRequest{{index}}(),
                """);
        }
        code.AppendLine("""
                    },
                    new global::System.Collections.Generic.Dictionary<global::System.Type, global::SwiftDispatch.NotificationDispatcher>
                    {
            """);
        for (int index = 0; index < model.Notifications.Count; index++)
        {
            code.AppendLine($$"""
                            [typeof({{model.Notifications[index].Message}})] = new __SwiftDispatchNotification{{index}}(),
                """);
        }
        code.AppendLine("""
                    });
            """);
    }
}
