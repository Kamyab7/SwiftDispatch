using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SwiftDispatch.Contracts;
using SwiftDispatch.Sample.CleanArchitecture.Application.Common.Behaviours;

namespace SwiftDispatch.Sample.CleanArchitecture.Application;

[GenerateSwiftDispatch(
    PipelineBehaviors = new[] { typeof(LoggingBehaviour<,>), typeof(ValidationBehaviour<,>) })]
public static partial class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services.AddSwiftDispatch();
    }
}
