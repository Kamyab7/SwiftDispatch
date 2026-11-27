using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TrueMediator.PackageSmoke.Behaviors;
using TrueMediator.Contracts;
using TrueMediator.PackageSmoke.Configuration;
using TrueMediator.PackageSmoke.Infrastructure;
using TrueMediator.PackageSmoke.Notifications;
using TrueMediator.PackageSmoke.Requests;

var services = new ServiceCollection();
services.AddScoped(static _ => new TraceLog());
services.AddScoped<IValidator<Hello>, HelloValidator>();
services.AddScoped<IValidator<SaveGreeting>, SaveGreetingValidator>();
services.AddPackageDispatch();
services.AddPackageDispatch(); // Verify idempotency: repeated registration must not duplicate services or notification delivery.

await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
await using AsyncServiceScope scope = provider.CreateAsyncScope();
IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
if (!ReferenceEquals(mediator, scope.ServiceProvider.GetRequiredService<ISender>())
    || !ReferenceEquals(mediator, scope.ServiceProvider.GetRequiredService<IPublisher>()))
{
    throw new InvalidOperationException("Mediator aliases must share the scope.");
}

string greeting = await mediator.Send(new Hello("AOT"));
await mediator.Send(new SaveGreeting(greeting));
await mediator.Publish(new GreetingSaved(greeting));
string[] expected = ["before", "hello", "after", "before", "saved: Hello, AOT!", "after", "audit", "email"];
TraceLog trace = scope.ServiceProvider.GetRequiredService<TraceLog>();
if (greeting != "Hello, AOT!" || !trace.Entries.SequenceEqual(expected))
{
    throw new InvalidOperationException("Generated dispatch produced unexpected results: " + string.Join(", ", trace.Entries));
}

int handlerCount = trace.Entries.Count(entry => entry == "hello" || entry.StartsWith("saved: ", StringComparison.Ordinal));
try
{
    await mediator.Send(new Hello(""));
    throw new InvalidOperationException("Invalid request reached its handler.");
}
catch (ValidationException exception) when (exception.Errors.Single().ErrorCode == "NameRequired")
{
}
try
{
    await mediator.Send(new SaveGreeting(""));
    throw new InvalidOperationException("Invalid void request reached its handler.");
}
catch (ValidationException)
{
}
if (trace.Entries.Count(entry => entry == "hello" || entry.StartsWith("saved: ", StringComparison.Ordinal)) != handlerCount)
{
    throw new InvalidOperationException("Validation did not prevent handler execution.");
}

Console.WriteLine(greeting);
Console.WriteLine(string.Join(" -> ", trace.Entries));
Console.WriteLine("TrueMediator smoke test passed.");
