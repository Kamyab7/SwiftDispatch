using Microsoft.CodeAnalysis;
using Xunit;
using static TrueMediator.Tests.TestSupport.GeneratorTestHelper;

namespace TrueMediator.Tests.Generators;

public sealed class FluentRegistrationTests
{
    private const string Registration = """
        public static class Setup
        {
            public static IServiceCollection Register(IServiceCollection services) =>
                services.AddTrueMediator(options => options
                    .AddHandlersFromAssemblyContaining<Ping>());
        }
        """;

    [Theory]
    [InlineData("ParallelNotificationExecutionStrategy")]
    [InlineData("SequentialNotificationExecutionStrategy")]
    public void SelectsNotificationStrategyInOptions(string strategy)
    {
        string generated = SuccessfulSource(Handler + """
            public static class Setup
            {
                public static IServiceCollection Register(IServiceCollection services) =>
                    services.AddTrueMediator(options => options
                        .AddHandlersFromAssemblyContaining<Ping>()
                        .UseNotificationExecutionStrategy<TrueMediator.
            """ + strategy + ">()); }");
        Assert.Contains("TryAddScoped<global::TrueMediator.INotificationExecutionStrategy>", generated);
        Assert.Contains("new global::TrueMediator." + strategy + "()", generated);
    }

    [Fact]
    public void SelectedCustomStrategySupportsConstructorInjection()
    {
        string generated = SuccessfulSource(Handler + """
            public class Strategy : TrueMediator.INotificationExecutionStrategy
            {
                public Strategy(IServiceProvider services) { }
                public Task Execute(System.Collections.Generic.IEnumerable<Func<CancellationToken, Task>> handlers,
                    CancellationToken token) => Task.CompletedTask;
            }
            public static class Setup
            {
                public static IServiceCollection Register(IServiceCollection services) =>
                    services.AddTrueMediator(options => options.UseNotificationExecutionStrategy<Strategy>());
            }
            """);
        Assert.Contains("new global::Strategy(global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::System.IServiceProvider>(sp))", generated);
    }

    [Fact]
    public void FluentRegistrationCompilesWithoutAttributeOrPartialClass()
    {
        string source = "using Microsoft.Extensions.DependencyInjection;\n" + Handler + Registration;
        string generated = SuccessfulSource(source);
        Assert.Equal(generated, SuccessfulSource(source));
        Assert.Contains("namespace Microsoft.Extensions.DependencyInjection;", generated);
        Assert.Contains("new global::Handler()", generated);
        Assert.DoesNotContain("System.Reflection", generated);
        Assert.DoesNotContain("MakeGenericType", generated);
    }

    [Fact]
    public void DiscoversReferencedAssemblyAndPreservesBehaviorOrder()
    {
        string generated = SuccessfulSource("""
            using Microsoft.Extensions.DependencyInjection;
            public class First<TRequest,TResponse> : IPipelineBehavior<TRequest,TResponse> where TRequest : notnull
            {
                public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct) => next();
            }
            public class Second<TRequest,TResponse> : IPipelineBehavior<TRequest,TResponse> where TRequest : notnull
            {
                public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct) => next();
            }
            public static class Setup
            {
                public static IServiceCollection Register(IServiceCollection services) =>
                    services.AddTrueMediator(options => options
                        .AddHandlersFromAssemblyContaining<Lib.Marker>()
                        .AddPipelineBehavior(typeof(First<,>))
                        .AddPipelineBehavior(typeof(Second<,>)));
            }
            """, Library("public"));
        Assert.Contains("new global::Lib.Handler()", generated);
        Assert.Contains("behavior0 = global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::First<global::Lib.Ping, int>>", generated);
        Assert.Contains("behavior1 = global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::Second<global::Lib.Ping, int>>", generated);
    }

    [Theory]
    [InlineData("options => { if (true) options.AddHandlersFromAssemblyContaining<Ping>(); }")]
    [InlineData("options => Configure(options)")]
    [InlineData("options => options.AddPipelineBehavior(behavior)")]
    [InlineData("options => options.AddHandlersFromAssemblyContaining<Setup>()")]
    public void RejectsConfigurationThatCannotBeReadStatically(string configuration)
    {
        var (result, _) = Generate("using Microsoft.Extensions.DependencyInjection;\n" + Handler + """
            public static class Setup
            {
                private static Type behavior = typeof(object);
                private static TrueMediatorOptions Configure(TrueMediatorOptions options) => options;
                public static IServiceCollection Register(IServiceCollection services) =>
                    services.AddTrueMediator(
            """ + configuration + "); }");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "TM001");
        Assert.Empty(Assert.Single(result.Results).GeneratedSources);
    }

    [Fact]
    public void RejectsConflictingConfigurations()
    {
        const string extra = "public static class SecondSetup { public static IServiceCollection Register(IServiceCollection services) => services.AddTrueMediator(options => options.AddHandlersFromAssemblyContaining<Ping>()); }";
        var (result, _) = Generate("using Microsoft.Extensions.DependencyInjection;\n" + Handler + Registration + extra);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "TM001");
    }

    [Fact]
    public void BlockLambdaSupportsExecutingAssemblyAndOpenBehaviors()
    {
        string generated = SuccessfulSource(Handler + """
            public class Logging<TRequest,TResponse> : IPipelineBehavior<TRequest,TResponse> where TRequest : notnull
            {
                public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct) => next();
            }
            public static class Setup
            {
                public static IServiceCollection Register(IServiceCollection services) => services.AddTrueMediator(cfg =>
                {
                    cfg.RegisterServicesFromAssembly(System.Reflection.Assembly.GetExecutingAssembly());
                    cfg.AddOpenBehavior(typeof(Logging<,>));
                });
            }
            """);
        Assert.Contains("new global::Handler()", generated);
        Assert.Contains("Logging<global::Ping, int>", generated);
        Assert.DoesNotContain("System.Reflection", generated);
    }

    [Fact]
    public void TypeofAssemblyIncludesReferencedHandlersWithoutMarkerApi()
    {
        string generated = SuccessfulSource("""
            public static class Setup
            {
                public static IServiceCollection Register(IServiceCollection services) => services.AddTrueMediator((cfg) =>
                {
                    cfg.RegisterServicesFromAssembly(typeof(Lib.Handler).Assembly);
                });
            }
            """, Library("public"));
        Assert.Contains("new global::Lib.Handler()", generated);
    }

    [Theory]
    [InlineData("cfg => { var assembly = System.Reflection.Assembly.GetExecutingAssembly(); cfg.RegisterServicesFromAssembly(assembly); }")]
    [InlineData("cfg => cfg.RegisterServicesFromAssembly(assembly)")]
    [InlineData("cfg => cfg.RegisterServicesFromAssembly(System.Reflection.Assembly.GetCallingAssembly())")]
    [InlineData("cfg => cfg.RegisterServicesFromAssembly(FakeAssembly.GetExecutingAssembly())")]
    public void RejectsAssembliesThatCannotBeIdentifiedAtBuildTime(string configuration)
    {
        var (result, _) = Generate("""
            public static class FakeAssembly
            {
                public static System.Reflection.Assembly GetExecutingAssembly() => typeof(FakeAssembly).Assembly;
            }
            public static class Setup
            {
                private static System.Reflection.Assembly assembly = typeof(Setup).Assembly;
                public static IServiceCollection Register(IServiceCollection services) => services.AddTrueMediator(
            """ + configuration + "); }");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "TM001");
        Assert.Empty(Assert.Single(result.Results).GeneratedSources);
    }

    [Fact]
    public void UnrelatedMethodDoesNotTriggerGeneration()
    {
        var (result, output) = Generate("""
            public class Other
            {
                public void AddTrueMediator(Action<object> configure) { }
                public void Run() => this.AddTrueMediator(options => Console.WriteLine(options));
            }
            """);
        Assert.Empty(result.Diagnostics);
        Assert.Empty(Assert.Single(result.Results).GeneratedSources);
        Assert.DoesNotContain(output.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }
}
