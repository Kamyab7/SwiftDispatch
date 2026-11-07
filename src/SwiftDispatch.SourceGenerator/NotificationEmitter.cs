using System.Collections.Generic;
using System.Text;

namespace SwiftDispatch.SourceGenerator;

internal static class NotificationEmitter
{
    internal static void Emit(StringBuilder code, IReadOnlyList<NotificationDispatchModel> notifications)
    {
        for (int index = 0; index < notifications.Count; index++)
        {
            code.Append("    private sealed class __SwiftDispatchNotification").Append(index).AppendLine(" : global::SwiftDispatch.NotificationDispatcher\n    {");
            code.AppendLine("        public override global::System.Threading.Tasks.Task Invoke(object value, global::System.IServiceProvider sp, global::System.Threading.CancellationToken cancellationToken)\n        {");
            code.Append("            var notification = (").Append(notifications[index].Message).AppendLine(")value;");
            code.AppendLine("            return global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::SwiftDispatch.INotificationExecutionStrategy>(sp).Execute(new global::System.Func<global::System.Threading.CancellationToken, global::System.Threading.Tasks.Task>[]\n            {");
            int handlerIndex = 0;
            foreach (string handlerResolution in notifications[index].HandlerResolutions)
            {
                code.AppendLine("                cancellationToken =>\n                {");
                code.Append("            global::SwiftDispatch.Contracts.INotificationHandler<").Append(notifications[index].Message)
                    .Append("> handler").Append(handlerIndex).Append(" = ").Append(handlerResolution).AppendLine(";");
                code.Append("            return handler").Append(handlerIndex++).AppendLine(".Handle(notification, cancellationToken);\n                },");
            }
            code.AppendLine("            }, cancellationToken);\n        }\n    }");
        }
    }
}
