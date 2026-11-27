using System.Text;

namespace TrueMediator.SourceGenerator;

internal static class NotificationEmitter
{
    internal static void Emit(StringBuilder code, NotificationDispatchModel notification, int index)
    {
        code.AppendLine($$"""
                private sealed class __TrueMediatorNotification{{index}} : global::TrueMediator.NotificationDispatcher
                {
                    public override global::System.Threading.Tasks.Task Invoke(object value, global::System.IServiceProvider sp, global::System.Threading.CancellationToken cancellationToken)
                    {
                        var notification = ({{notification.Message}})value;
                        return global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::TrueMediator.INotificationExecutionStrategy>(sp).Execute(new global::System.Func<global::System.Threading.CancellationToken, global::System.Threading.Tasks.Task>[]
                        {
            """);
        for (int handlerIndex = 0; handlerIndex < notification.HandlerResolutions.Count; handlerIndex++)
        {
            code.AppendLine($$"""
                                cancellationToken =>
                                {
                                    global::TrueMediator.Contracts.INotificationHandler<{{notification.Message}}> handler{{handlerIndex}} = {{notification.HandlerResolutions[handlerIndex]}};
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
