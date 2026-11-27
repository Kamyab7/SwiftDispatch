using System.Reflection;
using TrueMediator.FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TrueMediator.PackageSmoke.Behaviors;

namespace TrueMediator.PackageSmoke.Configuration;

internal static class DispatchRegistration
{
    internal static IServiceCollection AddPackageDispatch(this IServiceCollection services) =>
        services.AddTrueMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            options.AddOpenBehavior(typeof(LoggingBehavior<,>));
            options.AddOpenBehavior(typeof(FluentValidationBehavior<,>));
        });
}
