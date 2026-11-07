using System.Text;

namespace SwiftDispatch.SourceGenerator;

internal static class RegistrationEmitter
{
    internal static void Emit(StringBuilder code, DispatchModel model)
    {
        code.AppendLine("namespace Microsoft.Extensions.DependencyInjection;");
        code.AppendLine("internal static class SwiftDispatchGeneratedServiceCollectionExtensions\n{");
        code.AppendLine("    public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddSwiftDispatch(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services, global::System.Action<global::Microsoft.Extensions.DependencyInjection.SwiftDispatchOptions> configure)\n    {");
        code.AppendLine("        if (configure is null) throw new global::System.ArgumentNullException(nameof(configure));");
        code.AppendLine("        if (services is null) throw new global::System.ArgumentNullException(nameof(services));");
        foreach (ComponentRegistrationModel component in model.Components)
        {
            string type = component.TypeName;
            code.Append("        global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddTransient<")
                .Append(type).Append(">(services, static sp => new ").Append(type).Append('(');
            code.Append(string.Join(", ", component.ConstructorArguments));
            code.AppendLine("));");
        }
        if (model.NotificationStrategyResolution == null)
        {
            code.AppendLine("        global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddSingleton<global::SwiftDispatch.INotificationExecutionStrategy, global::SwiftDispatch.SequentialNotificationExecutionStrategy>(services);");
        }
        else
        {
            code.Append("        global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddScoped<global::SwiftDispatch.INotificationExecutionStrategy>(services, static sp => ")
                .Append(model.NotificationStrategyResolution).AppendLine(");");
        }
        code.AppendLine("        global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddSingleton<global::SwiftDispatch.DispatchTable>(services, static _ => CreateSwiftDispatchTable());");
        code.AppendLine("        global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddScoped<global::SwiftDispatch.Mediator>(services, static sp => new global::SwiftDispatch.Mediator(sp, global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::SwiftDispatch.DispatchTable>(sp)));");
        foreach (string contract in new[] { "IMediator", "ISender", "IPublisher" })
        {
            code.Append("        global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddScoped<global::SwiftDispatch.Contracts.")
                .Append(contract).AppendLine(">(services, static sp => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::SwiftDispatch.Mediator>(sp));");
        }
        code.AppendLine("        return services;\n    }");
    }
}
