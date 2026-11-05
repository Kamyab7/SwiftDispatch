using Microsoft.CodeAnalysis;
using SwiftDispatch.Tests.TestSupport;
using Xunit;

namespace SwiftDispatch.Tests.Generators;

public sealed class StreamGenerationTests
{
    private const string Stream = """
        public record Stream : IStreamRequest<int>;
        public class Handler : IStreamRequestHandler<Stream, int>
        {
            public System.Collections.Generic.IAsyncEnumerable<int> Handle(Stream request, CancellationToken ct) => null!;
        }
        """;

    [Fact]
    public void OpenStreamBehaviorsAreGenerated()
    {
        string source = GeneratorTestHelper.SuccessfulSource(Stream + """
            public class Behavior<TRequest,TResponse> : IStreamPipelineBehavior<TRequest,TResponse> where TRequest : notnull
            {
                public System.Collections.Generic.IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken ct) => next();
            }
            internal class Registration { public static IServiceCollection Register(IServiceCollection services) => services.AddSwiftDispatch(options => options.AddHandlersFromAssemblyContaining<Registration>().AddStreamPipelineBehavior(typeof(Behavior<,>))); }
            """);
        Assert.Contains("IStreamRequestDispatcher<int>", source);
        Assert.Contains("IStreamPipelineBehavior<global::Stream, int>", source);
        Assert.DoesNotContain("IRequestDispatcher<int>", source);
    }

    [Fact]
    public void DuplicateStreamHandlersAreRejected()
    {
        var (result, _) = GeneratorTestHelper.Generate(Stream + """
            public class Other : IStreamRequestHandler<Stream, int>
            {
                public System.Collections.Generic.IAsyncEnumerable<int> Handle(Stream request, CancellationToken ct) => null!;
            }
            internal class Registration { public static IServiceCollection Register(IServiceCollection services) => services.AddSwiftDispatch(options => options.AddHandlersFromAssemblyContaining<Registration>()); }
            """);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "SD004");
    }

    [Fact]
    public void OrdinaryBehaviorsCannotBeConfiguredAsStreamBehaviors()
    {
        var (result, _) = GeneratorTestHelper.Generate(Stream + """
            public class Behavior<TRequest,TResponse> : IPipelineBehavior<TRequest,TResponse> where TRequest : notnull
            {
                public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct) => next();
            }
            internal class Registration { public static IServiceCollection Register(IServiceCollection services) => services.AddSwiftDispatch(options => options.AddHandlersFromAssemblyContaining<Registration>().AddStreamPipelineBehavior(typeof(Behavior<,>))); }
            """);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "SD006");
    }
}
