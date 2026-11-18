using Microsoft.CodeAnalysis;
using Xunit;

using static SwiftDispatch.Tests.TestSupport.GeneratorTestHelper;

namespace SwiftDispatch.Tests.Generators;

public sealed class DispatchCatalogGenerationTests
{
    private static string Register(string options) => """
        internal class Registration
        {
            public static IServiceCollection Register(IServiceCollection services) => services.AddSwiftDispatch(options => {
        """ + options + "}); }";

    private static string Catalog(string source, params MetadataReference[] references)
    {
        SuccessfulSource(source, references);
        var (result, _) = Generate(source, references);
        return Assert.Single(Assert.Single(result.Results).GeneratedSources,
            source => source.HintName == "SwiftDispatch.DispatchCatalog.g.cs").SourceText.ToString();
    }

    [Theory]
    [InlineData("")]
    [InlineData("options.EnableDispatchCatalog(false);")]
    [InlineData("options.EnableDispatchCatalog(true); options.EnableDispatchCatalog(false);")]
    public void DisabledCatalogEmitsNoImplementationOrRegistration(string options)
    {
        string source = SuccessfulSource(Handler + Register(options));
        Assert.DoesNotContain("IDispatchCatalog", source);
        Assert.DoesNotContain("__SwiftDispatchCatalog", source);
    }

    [Theory]
    [InlineData("options.EnableDispatchCatalog(true);")]
    [InlineData("options.EnableDispatchCatalog(false); options.EnableDispatchCatalog(true);")]
    [InlineData("options.EnableDispatchCatalog(true).ValidateNotificationHandlers(false);")]
    public void EnabledCatalogIsDeterministicAndShowsConfiguredHandler(string options)
    {
        string input = Handler + Register(options);
        string source = Catalog(input);
        Assert.Equal(source, Catalog(input));
        Assert.Contains("Request: global::Ping -> int", source);
        Assert.Contains("Handler.Handle: global::Handler [<source>:9]", source);
        Assert.Contains("CONFIGURED FLOW ONLY", source);
        Assert.Contains("TryAddSingleton<global::SwiftDispatch.Diagnostics.IDispatchCatalog>", SuccessfulSource(input));
    }

    [Theory]
    [InlineData("DateTime.Now.Ticks > 0")]
    [InlineData("Settings.GetEnabled()")]
    public void NonConstantOptionUsesConfigurationDiagnostic(string argument)
    {
        var (result, _) = Generate(Handler + "internal class Settings { public static bool GetEnabled() => true; }" + Register("options.EnableDispatchCatalog(" + argument + ");"));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "SD001" && diagnostic.GetMessage().Contains("compile-time Boolean constant"));
    }

    [Fact]
    public void ConstantBooleanAndEmptyCatalogCompile()
    {
        Catalog("internal class Settings { public const bool Enabled = true; }" + Register("options.EnableDispatchCatalog(Settings.Enabled);"));
    }

    [Fact]
    public void ReferencedHandlersHaveNoInventedSourceLocation()
    {
        string source = Catalog(Register("options.EnableDispatchCatalog(true); options.AddHandlersFromAssemblyContaining<Lib.Marker>();"), Library("public"));
        Assert.Contains("Handler.Handle: global::Lib.Handler [source location unavailable]", source);
        Assert.Contains("typeof(global::Lib.Handler)", source);
    }

    [Theory]
    [InlineData("", "global::SwiftDispatch.SequentialNotificationExecutionStrategy", true)]
    [InlineData("options.UseNotificationExecutionStrategy<SwiftDispatch.ParallelNotificationExecutionStrategy>();", "global::SwiftDispatch.ParallelNotificationExecutionStrategy", false)]
    [InlineData("options.UseNotificationExecutionStrategy<CustomStrategy>();", "global::CustomStrategy", false)]
    public void NotificationsPreserveSubscribersAndDescribeStrategy(string configuration, string strategy, bool sequential)
    {
        string source = Catalog("""
            public record Notice : INotification;
            public class ZHandler : INotificationHandler<Notice> {
                public Task Handle(Notice notice, CancellationToken ct) => Task.CompletedTask;
            }
            public class AHandler : INotificationHandler<Notice> {
                public Task Handle(Notice notice, CancellationToken ct) => Task.CompletedTask;
            }
            public class CustomStrategy : SwiftDispatch.INotificationExecutionStrategy {
                public Task Execute(System.Collections.Generic.IEnumerable<Func<CancellationToken, Task>> handlers, CancellationToken ct) => Task.CompletedTask;
            }
            """ + Register("options.EnableDispatchCatalog(true);" + configuration));
        Assert.Contains("Notification strategy: " + strategy, source);
        Assert.Contains(sequential ? "Subscribers execute sequentially" : "execution order is determined by the strategy", source);
        Assert.True(source.IndexOf("Handler.Handle: global::AHandler", StringComparison.Ordinal) < source.IndexOf("Handler.Handle: global::ZHandler", StringComparison.Ordinal));
    }

    [Fact]
    public void CatalogUsesApplicableClosedBehaviorsAndStreamPipelineOnly()
    {
        string source = Catalog(Handler + """
            public record Other : IRequest<int>;
            public class OtherHandler : IRequestHandler<Other, int> {
                public Task<int> Handle(Other request, CancellationToken ct) => Task.FromResult(2);
            }
            public class Behavior<TRequest,TResponse> : IPipelineBehavior<TRequest,TResponse> where TRequest : Ping {
                public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct) => next();
            }
            public record Stream : IStreamRequest<int>;
            public class StreamHandler : IStreamRequestHandler<Stream,int> {
                public System.Collections.Generic.IAsyncEnumerable<int> Handle(Stream request, CancellationToken ct) => throw new NotImplementedException();
            }
            public class StreamBehavior : IStreamPipelineBehavior<Stream,int> {
                public System.Collections.Generic.IAsyncEnumerable<int> Handle(Stream request, StreamHandlerDelegate<int> next, CancellationToken ct) => next();
            }
            """ + Register("options.EnableDispatchCatalog(true); options.AddOpenBehavior(typeof(Behavior<,>)); options.AddStreamPipelineBehavior(typeof(StreamBehavior));"));
        Assert.Contains("typeof(global::Behavior<global::Ping, int>)", source);
        Assert.DoesNotContain("Behavior<global::Other", source);
        Assert.Contains("Stream: global::Stream -> int", source);
        Assert.Contains("typeof(global::StreamBehavior)", source);
        Assert.True(source.IndexOf("Request: global::Other", StringComparison.Ordinal) < source.IndexOf("Request: global::Ping", StringComparison.Ordinal));
    }

    [Fact]
    public void ExplicitAndInheritedMethodsUseImplementationSourceLines()
    {
        string source = Catalog("""
            public record Explicit : IRequest<int>;
            public class ExplicitHandler : IRequestHandler<Explicit,int> {
                Task<int> IRequestHandler<Explicit,int>.Handle(Explicit request, CancellationToken ct) => Task.FromResult(1);
            }
            public record Inherited : IRequest<int>;
            public abstract class BaseHandler : IRequestHandler<Inherited,int> {
                public Task<int> Handle(Inherited request, CancellationToken ct) => Task.FromResult(1);
            }
            public class InheritedHandler : BaseHandler;
            """ + Register("options.EnableDispatchCatalog(true);"));
        Assert.Contains("Handler.Handle: global::ExplicitHandler [<source>:8]", source);
        Assert.Contains("Handler.Handle: global::InheritedHandler [<source>:12]", source);
    }

    [Fact]
    public void MissingHandlersStillFailWhenCatalogIsEnabled()
    {
        var (result, _) = Generate("public record Missing : IRequest<int>;" + Register("options.EnableDispatchCatalog(true);"));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "SD007");
        Assert.Empty(Assert.Single(result.Results).GeneratedSources);
    }

    [Fact]
    public void SourcePathsAreCommentsAndCannotInjectCode()
    {
        const string path = "/features/Order\n#invalid\u2028.cs";
        var (result, output) = GenerateAtPath(Handler + Register("options.EnableDispatchCatalog(true);"), path);
        Assert.Empty(result.Diagnostics);
        Assert.Empty(output.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        string source = Assert.Single(Assert.Single(result.Results).GeneratedSources,
            source => source.HintName == "SwiftDispatch.DispatchCatalog.g.cs").SourceText.ToString();
        Assert.Contains("Handler.Handle: global::Handler [/features/Order #invalid .cs:9]", source);
        Assert.DoesNotContain("\n#invalid", source);
        Assert.DoesNotContain("\"/features/", source);
    }

    [Fact]
    public void MultiContractHandlerUsesEachRequestsOwnMethodLocation()
    {
        string source = Catalog("""
            public record First : IRequest<int>;
            public record Second : IRequest<int>;
            public class BothHandler : IRequestHandler<First,int>, IRequestHandler<Second,int> {
                public Task<int> Handle(First request, CancellationToken ct) => Task.FromResult(1);
                public Task<int> Handle(Second request, CancellationToken ct) => Task.FromResult(2);
            }
            """ + Register("options.EnableDispatchCatalog(true);"));
        Assert.Contains("Handler.Handle: global::BothHandler [<source>:9]", source);
        Assert.Contains("Handler.Handle: global::BothHandler [<source>:10]", source);
    }

    [Fact]
    public void NullableResponsesCompileWithoutNullableTypeofExpressions()
    {
        Catalog("""
            public record NullableRequest : IRequest<string?>;
            public class NullableHandler : IRequestHandler<NullableRequest,string?> {
                public Task<string?> Handle(NullableRequest request, CancellationToken ct) => Task.FromResult<string?>(null);
            }
            """ + Register("options.EnableDispatchCatalog(true);"));
    }
}
