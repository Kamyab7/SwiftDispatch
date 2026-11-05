using Microsoft.Extensions.DependencyInjection;
using SwiftDispatch.Sample.CleanArchitecture.Application.Common.Interfaces;
using SwiftDispatch.Sample.CleanArchitecture.Infrastructure.Persistence;

namespace SwiftDispatch.Sample.CleanArchitecture.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        => services.AddSingleton<ITodoRepository, InMemoryTodoRepository>();
}
