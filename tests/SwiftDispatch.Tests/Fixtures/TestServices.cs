using Microsoft.Extensions.DependencyInjection;

namespace SwiftDispatch.Tests.Fixtures;

internal static class TestServices
{
    internal static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddScoped(static _ => new State());
        services.AddScoped(static _ => new DisposableDependency());
        services.AddTestDispatch();
        services.AddTestDispatch();
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }
}
