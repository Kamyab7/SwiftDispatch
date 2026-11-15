using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SwiftDispatch.Sample.CleanArchitecture.Application.Common.Behaviours;

namespace SwiftDispatch.Sample.CleanArchitecture.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services.AddSwiftDispatch(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            options.AddOpenBehavior(typeof(LoggingBehaviour<,>));
            options.AddOpenBehavior(typeof(ValidationBehaviour<,>));
        });
    }
}
