using Microsoft.CodeAnalysis;
using Xunit;

using static SwiftDispatch.Tests.TestSupport.GeneratorTestHelper;

namespace SwiftDispatch.Tests.Generators;

public sealed class ConfigurationDiagnosticTests
{
    [Theory]
    [InlineData("[GenerateSwiftDispatch] public partial class Registration {}", "SD001")]
    [InlineData("[GenerateSwiftDispatch] public static class Registration {}", "SD001")]
    [InlineData("[GenerateSwiftDispatch] public static partial class Registration<T> {}", "SD001")]
    [InlineData("public class Outer { [GenerateSwiftDispatch] public static partial class Registration {} }", "SD001")]
    [InlineData("[GenerateSwiftDispatch] public static partial class A {} [GenerateSwiftDispatch] public static partial class B {}", "SD001")]
    [InlineData("[GenerateSwiftDispatch(HandlerAssemblyMarkers = null!)] public static partial class Registration {}", "SD001")]
    [InlineData("[GenerateSwiftDispatch(PipelineBehaviors = new[] { typeof(string) })] public static partial class Registration {}", "SD006")]
    public void InvalidConfigurationHasActionableDiagnostics(string source, string expected)
    {
        (GeneratorDriverRunResult result, _) = Generate(source);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == expected && diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.All(result.Results, static result => Assert.Null(result.Exception));
    }
}
