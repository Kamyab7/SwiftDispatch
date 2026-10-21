using Microsoft.Extensions.DependencyInjection;

namespace SwiftDispatch.Tests.Fixtures;

internal static class TestServices
{
    internal static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddScoped(static _ => new State());
        services.AddScoped(static _ => new DisposableDependency());
        services.AddSwiftDispatch();
        services.AddSwiftDispatch();
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }
}
