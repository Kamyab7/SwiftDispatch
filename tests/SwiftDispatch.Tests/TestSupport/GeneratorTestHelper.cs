using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using SwiftDispatch.Contracts;
using SwiftDispatch.SourceGenerator;
using Xunit;

namespace SwiftDispatch.Tests.TestSupport;

internal static class GeneratorTestHelper
{
    private const string Prelude = """
        global using System;
        global using System.Threading;
        global using System.Threading.Tasks;
        global using SwiftDispatch.Contracts;
        """;

    internal const string Handler = """
        public record Ping : IRequest<int>;
        public class Handler : IRequestHandler<Ping,int>
        {
            public Task<int> Handle(Ping request, CancellationToken ct) => Task.FromResult(1);
        }
        """;

    private static readonly ImmutableArray<MetadataReference> References =
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
        .Concat([typeof(IRequest<>).Assembly.Location, typeof(Mediator).Assembly.Location, typeof(IServiceCollection).Assembly.Location])
        .Distinct(StringComparer.Ordinal)
        .Select(static path => MetadataReference.CreateFromFile(path)).ToImmutableArray<MetadataReference>();

    private static CSharpCompilation Compilation(string source, IEnumerable<MetadataReference>? extra = null, string assembly = "Consumer") =>
        CSharpCompilation.Create(assembly, [CSharpSyntaxTree.ParseText(Prelude + "\n" + source)],
            References.Concat(extra ?? []), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    internal static (GeneratorDriverRunResult Result, Compilation Output) Generate(string source, params MetadataReference[] extra)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new SwiftDispatchGenerator().AsSourceGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(Compilation(source, extra), out Compilation output, out _);
        return (driver.GetRunResult(), output);
    }

    internal static string SuccessfulSource(string source, params MetadataReference[] extra)
    {
        (GeneratorDriverRunResult result, Compilation output) = Generate(source, extra);
        Assert.Empty(result.Diagnostics);
        Assert.Empty(output.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Empty(output.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning
            && diagnostic.Location.SourceTree?.FilePath.EndsWith("SwiftDispatch.Registration.g.cs", StringComparison.Ordinal) == true));
        return Assert.Single(Assert.Single(result.Results).GeneratedSources).SourceText.ToString();
    }

    internal static PortableExecutableReference Library(string accessibility, string extra = "", string assembly = "Library")
    {
        CSharpCompilation compilation = Compilation("""
            namespace Lib;
            public class Marker;
            public record Ping : IRequest<int>;
            """ + accessibility + """
             class Handler : IRequestHandler<Ping,int>
            {
                public Task<int> Handle(Ping request, CancellationToken ct) => Task.FromResult(1);
            }
            """ + extra, assembly: assembly);
        using var stream = new MemoryStream();
        Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        return MetadataReference.CreateFromImage(stream.ToArray());
    }
}
