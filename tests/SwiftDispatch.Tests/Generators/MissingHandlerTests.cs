using Microsoft.CodeAnalysis;
using Xunit;
using static SwiftDispatch.Tests.TestSupport.GeneratorTestHelper;

namespace SwiftDispatch.Tests.Generators;

public sealed class MissingHandlerTests
{
    private const string Registration = """
        public class Setup
        {
            public static IServiceCollection Register(IServiceCollection services) =>
                services.AddSwiftDispatch(options => { });
        }
        """;

    [Theory]
    [InlineData("public record Missing : IRequest<int>;")]
    [InlineData("public record Missing : IRequest;")]
    [InlineData("public record Missing : IStreamRequest<int>;")]
    [InlineData("public readonly record struct Missing : IRequest<int>;")]
    [InlineData("public abstract record Base : IRequest<int>; public record Missing : Base;")]
    [InlineData("public class Outer { public record Missing : IRequest<int>; }")]
    public void MissingConcreteHandlersFailBuildByDefault(string request)
    {
        var (result, _) = Generate(request + Registration);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("SD007", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("Missing", diagnostic.GetMessage());
        Assert.Contains("ValidateMissingHandlers(false)", diagnostic.GetMessage());
        Assert.True(diagnostic.Location.IsInSource);
        Assert.Empty(Assert.Single(result.Results).GeneratedSources);
    }

    [Fact]
    public void UnusedAbstractAndOpenRequestsAndNotificationsDoNotRequireHandlers()
    {
        SuccessfulSource("""
            public interface ICustomRequest : IRequest<int>;
            public abstract record Base : IRequest<int>;
            public record Generic<T> : IRequest<T>;
            public class Outer<T> { public record Nested : IRequest<int>; }
            public record Notice : INotification;
            """ + Registration);
    }

    [Fact]
    public void ExistingHandlerPassesValidation()
    {
        SuccessfulSource(Handler + Registration);
    }

    [Fact]
    public void BaseHandlerDoesNotCoverDerivedRequest()
    {
        var (result, _) = Generate(Handler + "public record Derived : Ping;" + Registration);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("SD007", diagnostic.Id);
        Assert.Contains("Derived", diagnostic.GetMessage());
    }

    [Fact]
    public void HandlerForOneShapeDoesNotCoverAnotherShape()
    {
        var (result, _) = Generate(Handler.Replace("IRequest<int>;", "IRequest<int>, IStreamRequest<int>;", StringComparison.Ordinal) + Registration);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("SD007", diagnostic.Id);
        Assert.Contains("IStreamRequest<int>", diagnostic.GetMessage());
    }

    [Theory]
    [InlineData("false")]
    [InlineData("Enabled")]
    public void ValidationCanBeDisabledWithBooleanConstant(string value)
    {
        SuccessfulSource("""
            public record Missing : IRequest<int>;
            public class Setup
            {
                private const bool Enabled = false;
                public static IServiceCollection Register(IServiceCollection services) =>
                    services.AddSwiftDispatch(options => options.ValidateMissingHandlers(
            """ + value + ")); }");
    }

    [Fact]
    public void ValidationCanBeExplicitlyEnabledAndLastSettingWins()
    {
        var (result, _) = Generate("""
            public record Missing : IRequest<int>;
            public class Setup
            {
                public static IServiceCollection Register(IServiceCollection services) =>
                    services.AddSwiftDispatch(options => options.ValidateMissingHandlers(false).ValidateMissingHandlers(true));
            }
            """);
        Assert.Equal("SD007", Assert.Single(result.Diagnostics).Id);
    }

    [Fact]
    public void RuntimeSettingIsRejected()
    {
        var (result, _) = Generate("""
            public class Setup
            {
                private static bool Enabled = false;
                public static IServiceCollection Register(IServiceCollection services) =>
                    services.AddSwiftDispatch(options => options.ValidateMissingHandlers(Enabled));
            }
            """);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("SD001", diagnostic.Id);
        Assert.Contains("compile-time Boolean constant", diagnostic.GetMessage());
    }

    [Fact]
    public void IncludedReferencedRequestWithoutHandlerIsDiagnosedAtRegistration()
    {
        var (result, _) = Generate("""
            public class Setup
            {
                public static IServiceCollection Register(IServiceCollection services) =>
                    services.AddSwiftDispatch(options => options.AddHandlersFromAssemblyContaining<Lib.Marker>());
            }
            """, Library("public", "public record Missing : IRequest<int>;"));
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("SD007", diagnostic.Id);
        Assert.Contains("Lib.Missing", diagnostic.GetMessage());
        Assert.Contains("AddSwiftDispatch", diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan));
    }

    [Fact]
    public void UnselectedReferencedRequestsAreOutsideValidationScope()
    {
        SuccessfulSource(Registration, Library("public", "public record Missing : IRequest<int>;"));
    }

    [Fact]
    public void DisablingMissingValidationKeepsOtherErrorsEnabled()
    {
        var (result, _) = Generate("""
            public record Missing : IRequest<int>;
            public class Setup
            {
                public static IServiceCollection Register(IServiceCollection services) =>
                    services.AddSwiftDispatch(options => options.ValidateMissingHandlers(false).AddOpenBehavior(typeof(string)));
            }
            """);
        Assert.Equal("SD006", Assert.Single(result.Diagnostics).Id);
    }
}
