using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using static SwiftDispatch.SourceGenerator.SymbolHelpers;
using static SwiftDispatch.SourceGenerator.SourceEmitter;

namespace SwiftDispatch.SourceGenerator;

internal static class NotificationEmitter
{
    internal static void Emit(StringBuilder code, List<Observer> notifications)
    {
        var groups = notifications.GroupBy<Observer, ITypeSymbol>(static item => item.Message, SymbolEqualityComparer.Default)
            .OrderBy(static group => Name(group.Key!), StringComparer.Ordinal).ToList();
        for (int index = 0; index < groups.Count; index++)
        {
            code.Append("    private sealed class __SwiftDispatchNotification").Append(index).AppendLine(" : global::SwiftDispatch.NotificationDispatcher\n    {");
            code.AppendLine("        public override global::System.Threading.Tasks.Task Invoke(object value, global::System.IServiceProvider sp, global::System.Threading.CancellationToken cancellationToken)\n        {");
            code.Append("            var notification = (").Append(Name(groups[index].Key!)).AppendLine(")value;");
            code.AppendLine("            return global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::SwiftDispatch.INotificationExecutionStrategy>(sp).Execute(new global::System.Func<global::System.Threading.CancellationToken, global::System.Threading.Tasks.Task>[]\n            {");
            int handlerIndex = 0;
            foreach (Observer observer in groups[index])
            {
                code.AppendLine("                cancellationToken =>\n                {");
                code.Append("            global::SwiftDispatch.Contracts.INotificationHandler<").Append(Name(observer.Message))
                    .Append("> handler").Append(handlerIndex).Append(" = ").Append(Resolve(observer.Handler)).AppendLine(";");
                code.Append("            return handler").Append(handlerIndex++).AppendLine(".Handle(notification, cancellationToken);\n                },");
            }
            code.AppendLine("            }, cancellationToken);\n        }\n    }");
        }
    }
}
