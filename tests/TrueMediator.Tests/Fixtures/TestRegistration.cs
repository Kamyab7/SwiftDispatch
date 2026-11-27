using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using TrueMediator.Tests.Fixtures.Behaviors;

namespace TrueMediator.Tests.Fixtures;

internal static class TestRegistration
{
    internal static IServiceCollection AddTestDispatch(this IServiceCollection services) =>
        services.AddTrueMediator(options =>
        {
            // Runtime tests intentionally send requests without handlers.
            options.ValidateMissingHandlers(false);
            options.UseNotificationExecutionStrategy<SequentialNotificationExecutionStrategy>();
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            options.AddOpenBehavior(typeof(Outer<,>));
            options.AddOpenBehavior(typeof(Inner<,>));
            options.AddStreamPipelineBehavior(typeof(TrueMediator.Tests.Runtime.StreamBehavior));
        });
}
