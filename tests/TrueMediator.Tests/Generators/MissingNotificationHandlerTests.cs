using Microsoft.CodeAnalysis;
using Xunit;
using static TrueMediator.Tests.TestSupport.GeneratorTestHelper;

namespace TrueMediator.Tests.Generators;

public sealed class MissingNotificationHandlerTests
{
    private static string Registration(string options) => """
        public class Setup
        {
            public static IServiceCollection Register(IServiceCollection services) =>
                services.AddTrueMediator(options => { OPTIONS });
        }
        """.Replace("OPTIONS", options, StringComparison.Ordinal);

    [Theory]
    [InlineData("")]
    [InlineData("options.ValidateNotificationHandlers(false);")]
    [InlineData("options.ValidateNotificationHandlers(true).ValidateNotificationHandlers(false);")]
    public void MissingSubscribersAreAllowedByDefaultOrWhenDisabled(string options)
    {
        SuccessfulSource("public record Notice : INotification;" + Registration(options));
    }

    [Theory]
    [InlineData("public record Notice : INotification;")]
    [InlineData("public readonly record struct Notice : INotification;")]
    [InlineData("public abstract record Base : INotification; public record Notice : Base;")]
    public void EnabledValidationRejectsMissingSubscribers(string message)
    {
        var (result, _) = Generate(message + Registration("options.ValidateNotificationHandlers(true);"));
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TM008", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("Notice", diagnostic.GetMessage());
        Assert.Contains("ValidateNotificationHandlers(false)", diagnostic.GetMessage());
        Assert.Empty(Assert.Single(result.Results).GeneratedSources);
    }

    [Fact]
    public void MultipleSubscribersAreValid()
    {
        SuccessfulSource("""
            public record Notice : INotification;
            public class First : INotificationHandler<Notice>
            {
                public Task Handle(Notice notification, CancellationToken ct) => Task.CompletedTask;
            }
            public class Second : INotificationHandler<Notice>
            {
                public Task Handle(Notice notification, CancellationToken ct) => Task.CompletedTask;
            }
            public interface IEvent : INotification;
            public abstract record Base : INotification;
            public record Generic<T> : INotification;
            """ + Registration("options.ValidateNotificationHandlers(true);"));
    }

    [Fact]
    public void NotificationCheckIsIndependentOfRequestCheck()
    {
        var (result, _) = Generate("""
            public record Missing : IRequest<int>;
            public record Notice : INotification;
            """ + Registration("options.ValidateMissingHandlers(false).ValidateNotificationHandlers(true);"));
        Assert.Equal("TM008", Assert.Single(result.Diagnostics).Id);
    }

    [Fact]
    public void RequestValidationRemainsEnabledByDefault()
    {
        var (result, _) = Generate("public record Missing : IRequest<int>;" + Registration("options.ValidateNotificationHandlers(false);"));
        Assert.Equal("TM007", Assert.Single(result.Diagnostics).Id);
    }

    [Theory]
    [InlineData("options.AddHandlersFromAssemblyContaining<Lib.Marker>().ValidateNotificationHandlers(true);", true)]
    [InlineData("options.ValidateNotificationHandlers(true);", false)]
    public void ReferencedNotificationsFollowRegistrationScope(string options, bool included)
    {
        var library = Library("public", "public record Notice : INotification;");
        if (!included)
        {
            SuccessfulSource(Registration(options), library);
            return;
        }
        var (result, _) = Generate(Registration(options), library);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TM008", diagnostic.Id);
        Assert.Contains("Lib.Notice", diagnostic.GetMessage());
        Assert.True(diagnostic.Location.IsInSource);
    }

    [Fact]
    public void RuntimeOptionIsRejected()
    {
        var (result, _) = Generate("""
            public class Setup
            {
                private static bool Enabled = true;
                public static IServiceCollection Register(IServiceCollection services) =>
                    services.AddTrueMediator(options => options.ValidateNotificationHandlers(Enabled));
            }
            """);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TM001", diagnostic.Id);
        Assert.Contains("compile-time Boolean constant", diagnostic.GetMessage());
    }
}
