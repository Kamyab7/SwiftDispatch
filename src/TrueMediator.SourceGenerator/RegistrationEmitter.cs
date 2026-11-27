using System.Text;

namespace TrueMediator.SourceGenerator;

internal static class RegistrationEmitter
{
    internal static void Emit(StringBuilder code, DispatchModel model)
    {
        code.AppendLine("""
                public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddTrueMediator(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services, global::System.Action<global::Microsoft.Extensions.DependencyInjection.TrueMediatorOptions> configure)
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
                        global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddSingleton<global::TrueMediator.INotificationExecutionStrategy, global::TrueMediator.SequentialNotificationExecutionStrategy>(services);
                """);
        }
        else
        {
            code.AppendLine($$"""
                        global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddScoped<global::TrueMediator.INotificationExecutionStrategy>(services, static sp => {{model.NotificationStrategyResolution}});
                """);
        }
        code.AppendLine("""
                    global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddSingleton<global::TrueMediator.DispatchTable>(services, static _ => CreateTrueMediatorTable());
                    global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddScoped<global::TrueMediator.Mediator>(services, static sp => new global::TrueMediator.Mediator(sp, global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::TrueMediator.DispatchTable>(sp)));
            """);
        foreach (string contract in new[] { "IMediator", "ISender", "IPublisher" })
        {
            code.AppendLine($$"""
                        global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddScoped<global::TrueMediator.Contracts.{{contract}}>(services, static sp => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::TrueMediator.Mediator>(sp));
                """);
        }
        code.AppendLine("""
                    return services;
                }
            """);
    }
}
