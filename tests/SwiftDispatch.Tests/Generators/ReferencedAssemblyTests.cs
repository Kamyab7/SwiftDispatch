using Microsoft.CodeAnalysis;
using Xunit;

using static SwiftDispatch.Tests.TestSupport.GeneratorTestHelper;

namespace SwiftDispatch.Tests.Generators;

public sealed class ReferencedAssemblyTests
{
    [Fact]
    public void ReferencedAssembliesAreIncludedOnlyWhenExplicitlyConfigured()
    {
        PortableExecutableReference library = Library("public");
        string omitted = SuccessfulSource("[GenerateSwiftDispatch] public static partial class Registration {}", library);
        Assert.DoesNotContain("Lib.Handler", omitted);
        string included = SuccessfulSource("""
            [GenerateSwiftDispatch(HandlerAssemblyMarkers = new[] { typeof(Lib.Marker), typeof(Lib.Marker) })]
            public static partial class Registration {}
            """, library);
        Assert.Contains("new global::Lib.Handler()", included);
        Assert.Contains("typeof(global::Lib.Ping)", included);
    }

    [Fact]
    public void InaccessibleReferencedHandlersAreDiagnosed()
    {
        (GeneratorDriverRunResult result, _) = Generate("""
            [GenerateSwiftDispatch(HandlerAssemblyMarkers = new[] { typeof(Lib.Marker) })]
            public static partial class Registration {}
            """, Library("internal"));
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "SD002");
    }

    [Fact]
    public void CrossAssemblyObserverOrderIsStable()
    {
        PortableExecutableReference library = Library("public", """
            public record Notice : INotification;
            public class ZObserver : INotificationHandler<Notice>
            {
                public Task Handle(Notice notification, CancellationToken ct) => Task.CompletedTask;
            }
            """, "AAA.Library");
        string generated = SuccessfulSource("""
            public class AObserver : INotificationHandler<Lib.Notice>
            {
                public Task Handle(Lib.Notice notification, CancellationToken ct) => Task.CompletedTask;
            }
            [GenerateSwiftDispatch(HandlerAssemblyMarkers = new[] { typeof(Lib.Marker) })]
            public static partial class Registration {}
            """, library);
        Assert.True(generated.IndexOf("handler0 = global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::Lib.ZObserver>", StringComparison.Ordinal) >= 0);
        Assert.True(generated.IndexOf("handler1 = global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::AObserver>", StringComparison.Ordinal) >= 0);
    }
}
