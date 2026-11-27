using Microsoft.Extensions.DependencyInjection;
using TrueMediator.Sample.CleanArchitecture.Application.Common.Interfaces;
using TrueMediator.Sample.CleanArchitecture.Infrastructure.Persistence;

namespace TrueMediator.Sample.CleanArchitecture.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        => services.AddSingleton<ITodoRepository, InMemoryTodoRepository>();
}
