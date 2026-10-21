using Microsoft.CodeAnalysis;
using Xunit;

using static SwiftDispatch.Tests.TestSupport.GeneratorTestHelper;

namespace SwiftDispatch.Tests.Generators;

public sealed class BehaviorGenerationTests
{
    [Fact]
    public void GenericConstraintsFilterBehaviorClosures()
    {
        string generated = SuccessfulSource(Handler + """
            public record Text : IRequest<string>;
            public class TextHandler : IRequestHandler<Text,string>
            {
                public Task<string> Handle(Text request, CancellationToken ct) => Task.FromResult("text");
            }
            public interface ISpecial;
            public class ReferenceBehavior<T,R> : IPipelineBehavior<T,R> where T : notnull where R : class
            {
                public Task<R> Handle(T request, RequestHandlerDelegate<R> next, CancellationToken ct) => next();
            }
            public class ValueBehavior<T,R> : IPipelineBehavior<T,R> where T : IRequest<R> where R : unmanaged
            {
                public Task<R> Handle(T request, RequestHandlerDelegate<R> next, CancellationToken ct) => next();
            }
            public class SpecialBehavior<T,R> : IPipelineBehavior<T,R> where T : ISpecial where R : new()
            {
                public Task<R> Handle(T request, RequestHandlerDelegate<R> next, CancellationToken ct) => next();
            }
            [GenerateSwiftDispatch(PipelineBehaviors = new[] { typeof(ReferenceBehavior<,>), typeof(ValueBehavior<,>), typeof(SpecialBehavior<,>) })]
            internal static partial class Registration {}
            """);
        Assert.Contains("ReferenceBehavior<global::Text, string>", generated);
        Assert.DoesNotContain("ReferenceBehavior<global::Ping", generated);
        Assert.Contains("ValueBehavior<global::Ping, int>", generated);
        Assert.DoesNotContain("ValueBehavior<global::Text", generated);
        Assert.DoesNotContain("new global::SpecialBehavior", generated);
    }

    [Fact]
    public void NullableResponsesRetainAnnotationsAndFilterNonNullableConstraints()
    {
        string generated = SuccessfulSource("""
            public record Ping : IRequest<string?>;
            public class Handler : IRequestHandler<Ping,string?>
            {
                public Task<string?> Handle(Ping request, CancellationToken ct) => Task.FromResult<string?>(null);
            }
            public class Behavior<T,R> : IPipelineBehavior<T,R> where T : notnull where R : class
            {
                public Task<R> Handle(T request, RequestHandlerDelegate<R> next, CancellationToken ct) => next();
            }
            [GenerateSwiftDispatch(PipelineBehaviors = new[] { typeof(Behavior<,>) })]
            internal static partial class Registration {}
            """);
        Assert.Contains("IRequestDispatcher<string?>", generated);
        Assert.DoesNotContain("new global::Behavior", generated);
    }

    [Fact]
    public void MalformedOpenBehaviorIsRejectedWithoutRequests()
    {
        (GeneratorDriverRunResult result, _) = Generate("""
            public class Reversed<T,R> : IPipelineBehavior<R,T> where R : notnull
            {
                public Task<T> Handle(R request, RequestHandlerDelegate<T> next, CancellationToken ct) => next();
            }
            [GenerateSwiftDispatch(PipelineBehaviors = new[] { typeof(Reversed<,>) })]
            internal static partial class Registration {}
            """);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "SD006");
    }
}
