using System.Reflection;
using FluentValidation;
using TrueMediator.FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TrueMediator.Sample.CleanArchitecture.Application.Common.Behaviours;

namespace TrueMediator.Sample.CleanArchitecture.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services.AddTrueMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            options.AddOpenBehavior(typeof(LoggingBehaviour<,>));
            options.AddOpenBehavior(typeof(FluentValidationBehavior<,>));
        });
    }
}
