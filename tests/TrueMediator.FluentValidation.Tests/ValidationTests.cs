using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using TrueMediator.Contracts;
using TrueMediator.FluentValidation;
using Xunit;

namespace TrueMediator.FluentValidation.Tests;

public sealed class ValidationTests
{
    [Fact]
    public async Task NoValidatorsContinuesExactlyOnce()
    {
        int calls = 0;
        var behavior = new FluentValidationBehavior<Query, int>([]);
        int response = await behavior.Handle(new Query("valid"), () => Task.FromResult(++calls), default);
        Assert.Equal(1, response);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task AggregatesFailuresWithoutLosingMetadataOrCallingHandler()
    {
        var state = new object();
        var first = new ValidationFailure("Name", "Required")
        {
            ErrorCode = "required", Severity = Severity.Warning,
            AttemptedValue = "", CustomState = state,
            FormattedMessagePlaceholderValues = new Dictionary<string, object> { ["PropertyName"] = "Name" }
        };
        var second = new ValidationFailure("Name", "Too short") { ErrorCode = "length" };
        var firstValidator = new InlineValidator<Query>();
        firstValidator.RuleFor(query => query.Name).Custom((_, context) => context.AddFailure(first));
        var secondValidator = new InlineValidator<Query>();
        secondValidator.RuleFor(query => query.Name).Custom((_, context) => context.AddFailure(second));
        var behavior = new FluentValidationBehavior<Query, int>([firstValidator, secondValidator]);
        bool handled = false;

        ValidationException exception = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(new Query(""), () => { handled = true; return Task.FromResult(1); }, default));

        Assert.False(handled);
        ValidationFailure[] errors = exception.Errors.ToArray();
        Assert.Equal(2, errors.Length);
        Assert.Same(first, errors[0]);
        Assert.Same(second, errors[1]);
    }

    [Fact]
    public async Task AsyncRulesReceiveCancellationAndDoNotContinue()
    {
        using var source = new CancellationTokenSource();
        var validator = new InlineValidator<Query>();
        bool entered = false;
        validator.RuleFor(query => query.Name).MustAsync(async (_, token) =>
        {
            Assert.Equal(source.Token, token);
            entered = true;
            source.Cancel();
            await Task.Delay(Timeout.Infinite, token);
            return true;
        });
        var behavior = new FluentValidationBehavior<Query, int>([validator]);
        bool handled = false;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => behavior.Handle(new Query("valid"),
            () => { handled = true; return Task.FromResult(1); }, source.Token));
        Assert.True(entered);
        Assert.False(handled);
    }

    [Fact]
    public async Task PreCancelledRequestDoesNotInvokeValidatorsOrHandler()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var validator = new InlineValidator<Query>();
        bool validated = false;
        bool handled = false;
        validator.RuleFor(query => query.Name).Custom((_, _) => validated = true);
        var behavior = new FluentValidationBehavior<Query, int>([validator]);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => behavior.Handle(new Query("valid"),
            () => { handled = true; return Task.FromResult(1); }, source.Token));
        Assert.False(validated);
        Assert.False(handled);
    }

    [Fact]
    public async Task CancellationAfterValidatorStopsPipelineEvenWhenRuleIgnoresToken()
    {
        using var source = new CancellationTokenSource();
        var validator = new InlineValidator<Query>();
        validator.RuleFor(query => query.Name).Custom((_, _) => source.Cancel());
        var behavior = new FluentValidationBehavior<Query, int>([validator]);
        bool handled = false;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => behavior.Handle(new Query("valid"),
            () => { handled = true; return Task.FromResult(1); }, source.Token));
        Assert.False(handled);
    }

    [Fact]
    public async Task InfrastructureExceptionsArePreserved()
    {
        var failure = new InvalidOperationException("Database unavailable");
        var validator = new InlineValidator<Query>();
        validator.RuleFor(query => query.Name).MustAsync((_, _) => Task.FromException<bool>(failure));
        var behavior = new FluentValidationBehavior<Query, int>([validator]);
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(new Query("valid"), () => throw new Exception("Must not continue"), default));
        Assert.Same(failure, exception);
    }

    [Fact]
    public async Task HandlerExceptionsArePreserved()
    {
        var failure = new InvalidOperationException("Handler failed");
        var behavior = new FluentValidationBehavior<Query, int>([]);
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(new Query("valid"), () => Task.FromException<int>(failure), default));
        Assert.Same(failure, exception);
    }

    [Fact]
    public async Task GeneratedPipelineRunsScopedValidatorsSequentiallyAndInOrder()
    {
        await using ServiceProvider provider = CreateProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        Assert.Equal(42, await mediator.Send(new Query("valid")));
        var state = scope.ServiceProvider.GetRequiredService<ExecutionState>();
        Assert.Equal(["outer.before", "first.start", "first.end", "second.start", "second.end", "query", "outer.after"], state.Entries);
        await using AsyncServiceScope otherScope = provider.CreateAsyncScope();
        Assert.NotSame(state, otherScope.ServiceProvider.GetRequiredService<ExecutionState>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GeneratedPipelineRejectsInvalidRequestsIncludingVoid(bool isVoid)
    {
        await using ServiceProvider provider = CreateProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await Assert.ThrowsAsync<ValidationException>(async () =>
        {
            if (isVoid) await mediator.Send(new Command(""));
            else await mediator.Send(new Query(""));
        });
        var entries = scope.ServiceProvider.GetRequiredService<ExecutionState>().Entries;
        Assert.DoesNotContain("query", entries);
        Assert.DoesNotContain("command", entries);
        Assert.DoesNotContain("outer.after", entries);
    }

    [Fact]
    public async Task GeneratedPipelineAcceptsValidVoidRequest()
    {
        await using ServiceProvider provider = CreateProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new Command("valid"));
        Assert.Equal(["outer.before", "command", "outer.after"], scope.ServiceProvider.GetRequiredService<ExecutionState>().Entries);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddScoped<ExecutionState>();
        services.AddScoped<IValidator<Query>, FirstValidator>();
        services.AddScoped<IValidator<Query>, SecondValidator>();
        services.AddScoped<IValidator<Command>, CommandValidator>();
        services.AddTrueMediator(options =>
        {
            options.RegisterServicesFromAssembly(typeof(Query).Assembly);
            options.AddOpenBehavior(typeof(OuterBehavior<,>));
            options.AddOpenBehavior(typeof(FluentValidationBehavior<,>));
        });
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }
}

public sealed record Query(string Name) : IRequest<int>;
public sealed record Command(string Name) : IRequest;

public sealed class ExecutionState
{
    public List<string> Entries { get; } = [];
    public bool ValidatorActive { get; set; }
}

public sealed class QueryHandler(ExecutionState state) : IRequestHandler<Query, int>
{
    public Task<int> Handle(Query request, CancellationToken cancellationToken)
    {
        state.Entries.Add("query");
        return Task.FromResult(42);
    }
}

public sealed class CommandHandler(ExecutionState state) : IRequestHandler<Command>
{
    public Task Handle(Command request, CancellationToken cancellationToken)
    {
        state.Entries.Add("command");
        return Task.CompletedTask;
    }
}

public sealed class OuterBehavior<TRequest, TResponse>(ExecutionState state) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        state.Entries.Add("outer.before");
        TResponse response = await next();
        state.Entries.Add("outer.after");
        return response;
    }
}

public sealed class FirstValidator : AbstractValidator<Query>
{
    public FirstValidator(ExecutionState state)
    {
        RuleFor(query => query.Name).MustAsync(async (name, _) =>
        {
            Assert.False(state.ValidatorActive);
            state.ValidatorActive = true;
            state.Entries.Add("first.start");
            await Task.Yield();
            state.Entries.Add("first.end");
            state.ValidatorActive = false;
            return !string.IsNullOrEmpty(name);
        });
    }
}

public sealed class SecondValidator : AbstractValidator<Query>
{
    public SecondValidator(ExecutionState state)
    {
        RuleFor(query => query.Name).MustAsync(async (_, _) =>
        {
            Assert.False(state.ValidatorActive);
            Assert.Contains("first.end", state.Entries);
            state.ValidatorActive = true;
            state.Entries.Add("second.start");
            await Task.Yield();
            state.Entries.Add("second.end");
            state.ValidatorActive = false;
            return true;
        });
    }
}

public sealed class CommandValidator : AbstractValidator<Command>
{
    public CommandValidator() => RuleFor(command => command.Name).NotEmpty();
}
