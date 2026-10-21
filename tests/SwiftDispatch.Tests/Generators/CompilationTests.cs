using Microsoft.CodeAnalysis;
using Xunit;

using static SwiftDispatch.Tests.TestSupport.GeneratorTestHelper;

namespace SwiftDispatch.Tests.Generators;

public sealed class CompilationTests
{
    [Fact]
    public void GeneratedCodeCompilesAndIsDeterministic()
    {
        string source = Handler + "\n[GenerateSwiftDispatch] public static partial class Registration {}";
        string first = SuccessfulSource(source);
        Assert.Equal(first, SuccessfulSource(source));
        Assert.Contains("new global::Handler()", first);
        Assert.Contains("typeof(global::Ping)", first);
        Assert.DoesNotContain("Activator", first);
        Assert.DoesNotContain("MakeGenericType", first);
        Assert.DoesNotContain("System.Reflection", first);
    }

    [Fact]
    public void NoConfigurationProducesNoCode()
    {
        (GeneratorDriverRunResult result, Compilation output) = Generate(Handler);
        Assert.Empty(result.Diagnostics);
        Assert.Empty(Assert.Single(result.Results).GeneratedSources);
        Assert.Empty(output.GetDiagnostics().Where(static item => item.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void EmptyConfigurationCompiles()
    {
        SuccessfulSource("[GenerateSwiftDispatch] internal static partial class Registration {}");
    }

    [Fact]
    public void NamespacesAndEscapedIdentifiersCompile()
    {
        SuccessfulSource("""
            namespace @event;
            public record @class : IRequest<int>;
            internal class @switch : IRequestHandler<@class,int>
            {
                public Task<int> Handle(@class request, CancellationToken ct) => Task.FromResult(1);
            }
            [GenerateSwiftDispatch] internal static partial class @static {}
            """);
    }
}
