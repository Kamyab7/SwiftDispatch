using System.Collections.Generic;
using System.Text;

namespace SwiftDispatch.SourceGenerator;

internal static class NotificationEmitter
{
    internal static void Emit(StringBuilder code, IReadOnlyList<NotificationDispatchModel> notifications)
    {
        for (int index = 0; index < notifications.Count; index++)
        {
            NotificationDispatchModel notification = notifications[index];
            code.AppendLine($$"""
                    private sealed class __SwiftDispatchNotification{{index}} : global::SwiftDispatch.NotificationDispatcher
                    {
                        public override global::System.Threading.Tasks.Task Invoke(object value, global::System.IServiceProvider sp, global::System.Threading.CancellationToken cancellationToken)
                        {
                            var notification = ({{notification.Message}})value;
                            return global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::SwiftDispatch.INotificationExecutionStrategy>(sp).Execute(new global::System.Func<global::System.Threading.CancellationToken, global::System.Threading.Tasks.Task>[]
                            {
                """);
            for (int handlerIndex = 0; handlerIndex < notification.HandlerResolutions.Count; handlerIndex++)
            {
                code.AppendLine($$"""
                                    cancellationToken =>
                                    {
                                        global::SwiftDispatch.Contracts.INotificationHandler<{{notification.Message}}> handler{{handlerIndex}} = {{notification.HandlerResolutions[handlerIndex]}};
                                        return handler{{handlerIndex}}.Handle(notification, cancellationToken);
                                    },
                    """);
            }
            code.AppendLine("""
                            }, cancellationToken);
                        }
                    }
                """);
        }
    }
}
