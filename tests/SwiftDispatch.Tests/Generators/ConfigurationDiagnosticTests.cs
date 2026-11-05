using Microsoft.CodeAnalysis;
using Xunit;

using static SwiftDispatch.Tests.TestSupport.GeneratorTestHelper;

namespace SwiftDispatch.Tests.Generators;

public sealed class ConfigurationDiagnosticTests
{
    [Theory]
    [InlineData("null!", "SD001")]
    [InlineData("Configure", "SD001")]
    [InlineData("options => options.AddPipelineBehavior(typeof(string))", "SD006")]
    [InlineData("options => options.AddStreamPipelineBehavior(typeof(string))", "SD006")]
    [InlineData("options => options.AddPipelineBehavior(typeof(int))", "SD006")]
    [InlineData("options => options.AddHandlersFromAssemblyContaining<Registration>()", "SD001")]
    public void InvalidConfigurationHasActionableDiagnostics(string configuration, string expected)
    {
        (GeneratorDriverRunResult result, _) = Generate("""
            public static class Registration
            {
                private static void Configure(SwiftDispatchOptions options) { }
                public static IServiceCollection Register(IServiceCollection services) =>
                    services.AddSwiftDispatch(
            """ + configuration + "); }");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == expected && diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.All(result.Results, static result => Assert.Null(result.Exception));
    }
}
