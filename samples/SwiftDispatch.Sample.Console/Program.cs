using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SwiftDispatch.Contracts;
using SwiftDispatch.Diagnostics;
using SwiftDispatch.Sample.Console;

var services = new ServiceCollection();
services.AddSwiftDispatch(options =>
{
    options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    options.EnableDispatchCatalog(true);
});

await using ServiceProvider provider = services.BuildServiceProvider();
await using AsyncServiceScope scope = provider.CreateAsyncScope();
IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

// Inspect the configured flow without executing the request or constructing its handler.
var flow = provider.GetRequiredService<IDispatchCatalog>().Find(typeof(Hello))[0];
Console.WriteLine($"Configured flow: {flow.MessageType.Name} -> {flow.HandlerTypes[0].Name}");

string name = args.Length > 0 ? args[0] : "Ada";
string greeting = await mediator.Send(new Hello(name));
Console.WriteLine(greeting);
await mediator.Publish(new GreetingCreated(greeting));
