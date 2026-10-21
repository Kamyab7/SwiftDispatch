using Microsoft.Extensions.DependencyInjection;
using SwiftDispatch.Contracts;
using SwiftDispatch.Sample.Configuration;
using SwiftDispatch.Sample.Infrastructure;
using SwiftDispatch.Sample.Notifications;
using SwiftDispatch.Sample.Requests;

var services = new ServiceCollection();
services.AddScoped(static _ => new TraceLog());
services.AddSwiftDispatch();
services.AddSwiftDispatch(); // Verify idempotency: repeated registration must not duplicate services or notification delivery.

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

Console.WriteLine(greeting);
Console.WriteLine(string.Join(" -> ", trace.Entries));
Console.WriteLine("SwiftDispatch smoke test passed.");
