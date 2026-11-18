using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SwiftDispatch.Tests.Fixtures.Behaviors;

namespace SwiftDispatch.Tests.Fixtures;

internal static class TestRegistration
{
    internal static IServiceCollection AddTestDispatch(this IServiceCollection services) =>
        services.AddSwiftDispatch(options =>
        {
            // Runtime tests intentionally send requests without handlers.
            options.ValidateMissingHandlers(false);
            options.EnableDispatchCatalog(true);
            options.UseNotificationExecutionStrategy<SequentialNotificationExecutionStrategy>();
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            options.AddOpenBehavior(typeof(Outer<,>));
            options.AddOpenBehavior(typeof(Inner<,>));
            options.AddStreamPipelineBehavior(typeof(SwiftDispatch.Tests.Runtime.StreamBehavior));
        });
}
