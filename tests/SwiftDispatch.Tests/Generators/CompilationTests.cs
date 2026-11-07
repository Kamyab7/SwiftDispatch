using Microsoft.CodeAnalysis;
using Xunit;

using static SwiftDispatch.Tests.TestSupport.GeneratorTestHelper;

namespace SwiftDispatch.Tests.Generators;

public sealed class CompilationTests
{
    [Fact]
    public void EmitsSeparateDeterministicFilesForEachDispatcher()
    {
        string source = Handler + """
            public record Notice : INotification;
            public class Observer : INotificationHandler<Notice>
            {
                public Task Handle(Notice notification, CancellationToken ct) => Task.CompletedTask;
            }
            public record Stream : IStreamRequest<int>;
            public class StreamHandler : IStreamRequestHandler<Stream, int>
            {
                public async System.Collections.Generic.IAsyncEnumerable<int> Handle(Stream request,
                    [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
                {
                    await Task.Yield();
                    yield return 1;
                }
            }
            public class Registration
            {
                public static IServiceCollection Register(IServiceCollection services) =>
                    services.AddSwiftDispatch(options => options.AddHandlersFromAssemblyContaining<Registration>());
            }
            """;
        SuccessfulSource(source);
        var (first, _) = Generate(source);
        var (second, _) = Generate(source);
        var files = Assert.Single(first.Results).GeneratedSources;
        Assert.Equal(new[]
        {
            "SwiftDispatch.Registration.g.cs",
            "SwiftDispatch.DispatchTable.g.cs",
            "SwiftDispatch.Request.0.g.cs",
            "SwiftDispatch.Stream.1.g.cs",
            "SwiftDispatch.Notification.0.g.cs"
        }, files.Select(file => file.HintName));
        Assert.Equal(files.Select(file => (file.HintName, file.SourceText.ToString())),
            Assert.Single(second.Results).GeneratedSources.Select(file => (file.HintName, file.SourceText.ToString())));
        Assert.DoesNotContain("private sealed class", files[0].SourceText.ToString());
        Assert.DoesNotContain("private sealed class", files[1].SourceText.ToString());
        Assert.All(files.Skip(2), file => Assert.Single(file.SyntaxTree.GetRoot().DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>(),
            type => type.Modifiers.Any(modifier => modifier.Text == "sealed")));
    }

    [Fact]
    public void GeneratedCodeCompilesAndIsDeterministic()
    {
        string source = Handler + "\npublic class Registration { public static IServiceCollection Register(IServiceCollection services) => services.AddSwiftDispatch(options => options.AddHandlersFromAssemblyContaining<Registration>()); }";
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
        SuccessfulSource("internal class Registration { public static IServiceCollection Register(IServiceCollection services) => services.AddSwiftDispatch(options => options.AddHandlersFromAssemblyContaining<Registration>()); }");
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
            internal class @static { public static IServiceCollection Register(IServiceCollection services) => services.AddSwiftDispatch(options => options.AddHandlersFromAssemblyContaining<@static>()); }
            """);
    }
}
