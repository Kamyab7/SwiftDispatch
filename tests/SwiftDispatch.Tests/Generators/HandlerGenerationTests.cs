using Microsoft.CodeAnalysis;
using Xunit;

using static SwiftDispatch.Tests.TestSupport.GeneratorTestHelper;

namespace SwiftDispatch.Tests.Generators;

public sealed class HandlerGenerationTests
{
    [Fact]
    public void DuplicateHandlersAreRejected()
    {
        (GeneratorDriverRunResult result, _) = Generate(Handler + """
            public class Other : IRequestHandler<Ping,int>
            {
                public Task<int> Handle(Ping request, CancellationToken ct) => Task.FromResult(2);
            }
            [GenerateSwiftDispatch] public static partial class Registration {}
            """);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "SD004");
    }

    [Theory]
    [InlineData("public Handler() {} public Handler(string value) {}")]
    [InlineData("private Handler() {}")]
    [InlineData("public Handler(ref int value) {}")]
    public void InvalidConstructorsAreRejected(string constructor)
    {
        (GeneratorDriverRunResult result, _) = Generate("""
            public record Ping : IRequest<int>;
            public class Handler : IRequestHandler<Ping,int>
            {
            """ + constructor + """
                public Task<int> Handle(Ping request, CancellationToken ct) => Task.FromResult(1);
            }
            [GenerateSwiftDispatch] public static partial class Registration {}
            """);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "SD003");
    }

    [Fact]
    public void OpenGenericHandlersAreRejected()
    {
        (GeneratorDriverRunResult result, _) = Generate("""
            public record Ping<T> : IRequest<T>;
            public class Handler<T> : IRequestHandler<Ping<T>,T>
            {
                public Task<T> Handle(Ping<T> request, CancellationToken ct) => Task.FromResult(default(T)!);
            }
            [GenerateSwiftDispatch] public static partial class Registration {}
            """);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "SD005");
    }

    [Fact]
    public void ExplicitInterfaceHandlersAndConstructorDependenciesCompile()
    {
        SuccessfulSource("""
            public record Ping : IRequest<int>;
            public class Handler(IServiceProvider services) : IRequestHandler<Ping,int>
            {
                Task<int> IRequestHandler<Ping,int>.Handle(Ping request, CancellationToken ct) => Task.FromResult(1);
            }
            [GenerateSwiftDispatch] internal static partial class Registration {}
            """);
    }

    [Fact]
    public void InheritedClosedHandlersAreDiscovered()
    {
        SuccessfulSource("""
            public record Ping : IRequest<int>;
            public abstract class Base<T> : IRequestHandler<Ping,int>
            {
                public Task<int> Handle(Ping request, CancellationToken ct) => Task.FromResult(1);
            }
            public class Handler : Base<int>;
            [GenerateSwiftDispatch] internal static partial class Registration {}
            """);
    }

    [Fact]
    public void NullableConstructorParametersCompileWithoutGeneratedWarnings()
    {
        SuccessfulSource("""
            public record Ping : IRequest<int>;
            public class Handler : IRequestHandler<Ping,int>
            {
                public Handler(IServiceProvider? services, int? value) {}
                public Task<int> Handle(Ping request, CancellationToken ct) => Task.FromResult(1);
            }
            [GenerateSwiftDispatch] internal static partial class Registration {}
            """);
    }

    [Fact]
    public void InheritedRequiredMembersProduceConstructorDiagnostic()
    {
        (GeneratorDriverRunResult result, _) = Generate("""
            public record Ping : IRequest<int>;
            public abstract class Base { public required string Name { get; init; } }
            public class Handler : Base, IRequestHandler<Ping,int>
            {
                public Task<int> Handle(Ping request, CancellationToken ct) => Task.FromResult(1);
            }
            [GenerateSwiftDispatch] internal static partial class Registration {}
            """);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "SD003");
    }
}
