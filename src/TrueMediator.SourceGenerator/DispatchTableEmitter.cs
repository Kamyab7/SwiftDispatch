using System.Text;

namespace TrueMediator.SourceGenerator;

internal static class DispatchTableEmitter
{
    internal static void Emit(StringBuilder code, DispatchModel model)
    {
        code.AppendLine("""
                private static global::TrueMediator.DispatchTable CreateTrueMediatorTable() => new(
                    new global::System.Collections.Generic.Dictionary<global::System.Type, object>
                    {
            """);
        for (int index = 0; index < model.Requests.Count; index++)
        {
            code.AppendLine($$"""
                            [typeof({{model.Requests[index].Message}})] = new __TrueMediatorRequest{{index}}(),
                """);
        }
        code.AppendLine("""
                    },
                    new global::System.Collections.Generic.Dictionary<global::System.Type, global::TrueMediator.NotificationDispatcher>
                    {
            """);
        for (int index = 0; index < model.Notifications.Count; index++)
        {
            code.AppendLine($$"""
                            [typeof({{model.Notifications[index].Message}})] = new __TrueMediatorNotification{{index}}(),
                """);
        }
        code.AppendLine("""
                    });
            """);
    }
}
