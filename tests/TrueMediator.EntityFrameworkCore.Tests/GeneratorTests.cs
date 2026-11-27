using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrueMediator.Contracts;
using TrueMediator.SourceGenerator;
using Xunit;

namespace TrueMediator.EntityFrameworkCore.Tests;

public sealed class GeneratorTests
{
    private const string Prelude = """
        using System;
        using System.Threading;
        using System.Threading.Tasks;
        using Microsoft.EntityFrameworkCore;
        using Microsoft.Extensions.DependencyInjection;
        using TrueMediator.Contracts;
        using TrueMediator.EntityFrameworkCore;
        public class Store : DbContext { }
        """;
    private const string Registration = """
        public static class Setup
        {
            public static IServiceCollection Register(IServiceCollection services) => services.AddTrueMediator(options =>
            {
                options.ValidateMissingHandlers(false);
                options.UseEntityFrameworkTransactions<Store>();
            });
        }
        """;
    private const string Handler = """
        public sealed class Handler : IRequestHandler<Message, int>
        {
            public Task<int> Handle(Message request, CancellationToken ct) => Task.FromResult(1);
        }
        """;

    [Theory]
    [InlineData("IQuery<int>")]
    [InlineData("INotification")]
    [InlineData("IStreamRequest<int>")]
    [InlineData("IDisposable")]
    public void InvalidTargetsProduceBuildErrorsAtAttribute(string contract)
    {
        string dispose = contract == "IDisposable" ? "{ public void Dispose() { } }" : ";";
        var (result, _) = Generate($"[Transactional] public record Message : {contract} {dispose}" + Registration);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TM009", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("Transactional", diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan));
    }

    [Fact]
    public void InheritedQueryContractIsRejectedEvenWithoutRegistration()
    {
        var (result, _) = Generate("public interface IRead<T> : IQuery<T>; [Transactional] public record Message : IRead<int>;");
        Assert.Equal("TM009", Assert.Single(result.Diagnostics).Id);
    }

    [Fact]
    public void MissingDefaultProducesClearDiagnostic()
    {
        var (result, _) = Generate("[Transactional] public record Message : ICommand<int>;" + Handler
            + Registration.Replace("options.UseEntityFrameworkTransactions<Store>();", ""));
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TM010", diagnostic.Id);
        Assert.Contains("default", diagnostic.GetMessage());
    }

    [Theory]
    [InlineData("typeof(string)")]
    [InlineData("typeof(AbstractStore)")]
    [InlineData("typeof(GenericStore<>)")]
    [InlineData("null")]
    public void InvalidExplicitContextProducesBuildError(string context)
    {
        var (result, _) = Generate("public abstract class AbstractStore : DbContext {} public class GenericStore<T> : DbContext {}"
            + $"[Transactional({context})] public record Message : ICommand<int>;" + Handler + Registration);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "TM010");
    }

    [Fact]
    public void InaccessibleContextIsRejected()
    {
        var (result, _) = Generate("""
            public class Container
            {
                private class HiddenStore : DbContext { }
                [Transactional(typeof(HiddenStore))] public record Message : IRequest<int>;
            }
            """ + Registration);
        Assert.Equal("TM010", Assert.Single(result.Diagnostics).Id);
    }

    [Theory]
    [InlineData("[Transactional] public abstract record Message : ICommand<int>;")]
    [InlineData("[Transactional] public record Message<T> : ICommand<int>;")]
    public void OpenOrAbstractTransactionTargetsAreRejected(string declaration)
    {
        var (result, _) = Generate(declaration + Registration);
        Assert.Equal("TM009", Assert.Single(result.Diagnostics).Id);
    }

    [Fact]
    public void ConflictingCommandQueryContractsAreRejectedWithoutEfAttribute()
    {
        var (result, _) = Generate("public record Message : ICommand<int>, IQuery<int>;" + Registration);
        Assert.Equal("TM011", Assert.Single(result.Diagnostics).Id);
    }

    [Fact]
    public void RepeatedDefaultConfigurationIsRejected()
    {
        var (result, _) = Generate(Registration.Replace("options.UseEntityFrameworkTransactions<Store>();",
            "options.UseEntityFrameworkTransactions<Store>(); options.UseEntityFrameworkTransactions<Store>();"));
        Assert.Equal("TM001", Assert.Single(result.Diagnostics).Id);
    }

    [Fact]
    public void ExplicitContextOverridesDefaultAndIsGeneratedInnermost()
    {
        string generated = Successful("""
            public class OtherStore : DbContext { }
            [Transactional(typeof(OtherStore))] public record Message : ICommand<int>;
            public class Before<TRequest,TResponse> : IPipelineBehavior<TRequest,TResponse> where TRequest : notnull
            {
                public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct) => next();
            }
            """ + Handler + Registration.Replace("options.UseEntityFrameworkTransactions<Store>();",
                "options.UseEntityFrameworkTransactions<Store>(); options.AddOpenBehavior(typeof(Before<,>));"));
        Assert.Contains("EntityFrameworkTransactionBehavior<global::Message, int, global::OtherStore>", generated);
        Assert.Contains("behavior1 = global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::TrueMediator.EntityFrameworkCore.EntityFrameworkTransactionBehavior", generated);
        Assert.Contains("behavior0 = global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::Before", generated);
    }

    [Fact]
    public void ExplicitContextDoesNotRequireDefaultConfiguration()
    {
        string generated = Successful("[Transactional(typeof(Store))] public record Message : IRequest<int>;" + Handler
            + Registration.Replace("options.UseEntityFrameworkTransactions<Store>();", ""));
        Assert.Contains("EntityFrameworkTransactionBehavior<global::Message, int, global::Store>", generated);
    }

    [Fact]
    public void UnmarkedQueryHasNoTransactionBehavior()
    {
        string generated = Successful("public record Message : IQuery<int>;" + Handler + Registration);
        Assert.DoesNotContain("EntityFrameworkTransactionBehavior", generated);
    }

    [Fact]
    public void MissingHandlerValidationRecognizesCommandAndQueryContracts()
    {
        var (result, _) = Generate("public record MissingCommand : ICommand; public record MissingQuery : IQuery<int>;"
            + Registration.Replace("options.ValidateMissingHandlers(false);", ""));
        Assert.Equal(2, result.Diagnostics.Length);
        Assert.All(result.Diagnostics, diagnostic => Assert.Equal("TM007", diagnostic.Id));
    }

    [Fact]
    public void ReferencedAttributesAreValidatedAndWiredWhenAssemblyIsIncluded()
    {
        Compilation library = Compilation("namespace Lib { public class LibraryStore : DbContext { } [Transactional(typeof(LibraryStore))] public record Message : ICommand<int>;"
            + "public class Handler : IRequestHandler<Message,int> { public Task<int> Handle(Message request,CancellationToken ct) => Task.FromResult(1); } }", "Library");
        using var stream = new MemoryStream();
        var emitted = library.Emit(stream);
        Assert.True(emitted.Success, string.Join("\n", emitted.Diagnostics));
        MetadataReference reference = MetadataReference.CreateFromImage(stream.ToArray());
        // Host and library have different context types; the attribute retains the library context.
        string generated = Successful(Registration.Replace("options.UseEntityFrameworkTransactions<Store>();",
            "options.RegisterServicesFromAssembly(typeof(Lib.Message).Assembly);"), reference);
        Assert.Contains("EntityFrameworkTransactionBehavior<global::Lib.Message, int, global::Lib.LibraryStore>", generated);
    }

    private static readonly ImmutableArray<MetadataReference> References =
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
        .Concat([typeof(IRequest<>).Assembly.Location, typeof(Mediator).Assembly.Location,
            typeof(TransactionalAttribute).Assembly.Location, typeof(DbContext).Assembly.Location,
            typeof(RelationalDatabaseFacadeExtensions).Assembly.Location, typeof(IServiceCollection).Assembly.Location])
        .Distinct().Select(path => MetadataReference.CreateFromFile(path)).ToImmutableArray<MetadataReference>();

    private static CSharpCompilation Compilation(string source, string name = "Consumer", params MetadataReference[] extra) =>
        CSharpCompilation.Create(name, [CSharpSyntaxTree.ParseText(Prelude + "\n" + source)], References.Concat(extra),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    private static (GeneratorDriverRunResult Result, Compilation Output) Generate(string source, params MetadataReference[] extra)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new TrueMediatorGenerator().AsSourceGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(Compilation(source, "Consumer", extra), out Compilation output, out _);
        return (driver.GetRunResult(), output);
    }

    private static string Successful(string source, params MetadataReference[] extra)
    {
        var (result, output) = Generate(source, extra);
        Assert.Empty(result.Diagnostics);
        Assert.Empty(output.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        return string.Join("\n", result.Results.SelectMany(result => result.GeneratedSources).Select(source => source.SourceText.ToString()));
    }
}
