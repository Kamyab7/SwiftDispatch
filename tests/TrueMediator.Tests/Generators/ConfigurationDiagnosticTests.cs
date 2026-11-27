using Microsoft.CodeAnalysis;
using Xunit;

using static TrueMediator.Tests.TestSupport.GeneratorTestHelper;

namespace TrueMediator.Tests.Generators;

public sealed class ConfigurationDiagnosticTests
{
    [Theory]
    [InlineData("options => options.UseNotificationExecutionStrategy<string>()", "TM001")]
    [InlineData("options => options.UseNotificationExecutionStrategy<TrueMediator.ParallelNotificationExecutionStrategy>().UseNotificationExecutionStrategy<TrueMediator.SequentialNotificationExecutionStrategy>()", "TM001")]
    [InlineData("null!", "TM001")]
    [InlineData("Configure", "TM001")]
    [InlineData("options => options.AddPipelineBehavior(typeof(string))", "TM006")]
    [InlineData("options => options.AddStreamPipelineBehavior(typeof(string))", "TM006")]
    [InlineData("options => options.AddPipelineBehavior(typeof(int))", "TM006")]
    [InlineData("options => options.AddHandlersFromAssemblyContaining<Registration>()", "TM001")]
    public void InvalidConfigurationHasActionableDiagnostics(string configuration, string expected)
    {
        (GeneratorDriverRunResult result, _) = Generate("""
            public static class Registration
            {
                private static void Configure(TrueMediatorOptions options) { }
                public static IServiceCollection Register(IServiceCollection services) =>
                    services.AddTrueMediator(
            """ + configuration + "); }");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == expected && diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.All(result.Results, static result => Assert.Null(result.Exception));
    }
}
