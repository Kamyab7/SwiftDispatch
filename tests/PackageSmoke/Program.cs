using Microsoft.Extensions.DependencyInjection;
using SwiftDispatch.Contracts;
using SwiftDispatch.Diagnostics;
using SwiftDispatch.PackageSmoke.Configuration;
using SwiftDispatch.PackageSmoke.Infrastructure;
using SwiftDispatch.PackageSmoke.Notifications;
using SwiftDispatch.PackageSmoke.Requests;

var services = new ServiceCollection();
services.AddScoped(static _ => new TraceLog());
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

var catalog = provider.GetRequiredService<IDispatchCatalog>();
var helloFlow = catalog.Find(typeof(Hello));
if (helloFlow.Count != 1 || helloFlow[0].ResponseType != typeof(string)
    || helloFlow[0].HandlerTypes.Count != 1 || helloFlow[0].BehaviorTypes.Count != 2
    || catalog.Find(typeof(SaveGreeting))[0].ResponseType != null
    || catalog.Find(typeof(GreetingSaved))[0].HandlerTypes.Count != 2
    || !ReferenceEquals(catalog, scope.ServiceProvider.GetRequiredService<IDispatchCatalog>()))
{
    throw new InvalidOperationException("Generated dispatch catalog produced unexpected metadata.");
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
