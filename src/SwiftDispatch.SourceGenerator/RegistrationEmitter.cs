using System.Text;

namespace SwiftDispatch.SourceGenerator;

internal static class RegistrationEmitter
{
    internal static void Emit(StringBuilder code, DispatchModel model)
    {
        code.AppendLine("""
                public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddSwiftDispatch(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services, global::System.Action<global::Microsoft.Extensions.DependencyInjection.SwiftDispatchOptions> configure)
                {
                    if (configure is null) throw new global::System.ArgumentNullException(nameof(configure));
                    if (services is null) throw new global::System.ArgumentNullException(nameof(services));
            """);
        foreach (ComponentRegistrationModel component in model.Components)
        {
            string arguments = string.Join(", ", component.ConstructorArguments);
            code.AppendLine($$"""
                        global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddTransient<{{component.TypeName}}>(services, static sp => new {{component.TypeName}}({{arguments}}));
                """);
        }
        if (model.NotificationStrategyResolution == null)
        {
            code.AppendLine("""
                        global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddSingleton<global::SwiftDispatch.INotificationExecutionStrategy, global::SwiftDispatch.SequentialNotificationExecutionStrategy>(services);
                """);
        }
        else
        {
            code.AppendLine($$"""
                        global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddScoped<global::SwiftDispatch.INotificationExecutionStrategy>(services, static sp => {{model.NotificationStrategyResolution}});
                """);
        }
        code.AppendLine("""
                    global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddSingleton<global::SwiftDispatch.DispatchTable>(services, static _ => CreateSwiftDispatchTable());
                    global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddScoped<global::SwiftDispatch.Mediator>(services, static sp => new global::SwiftDispatch.Mediator(sp, global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::SwiftDispatch.DispatchTable>(sp)));
            """);
        foreach (string contract in new[] { "IMediator", "ISender", "IPublisher" })
        {
            code.AppendLine($$"""
                        global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddScoped<global::SwiftDispatch.Contracts.{{contract}}>(services, static sp => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::SwiftDispatch.Mediator>(sp));
                """);
        }
        code.AppendLine("""
                    return services;
                }
            """);
    }
}
