using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SwiftDispatch.PackageSmoke.Behaviors;

namespace SwiftDispatch.PackageSmoke.Configuration;

internal static class DispatchRegistration
{
    internal static IServiceCollection AddPackageDispatch(this IServiceCollection services) =>
        services.AddSwiftDispatch(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            options.AddOpenBehavior(typeof(LoggingBehavior<,>));
            options.AddPipelineBehavior(typeof(ValidationBehavior));
        });
}
