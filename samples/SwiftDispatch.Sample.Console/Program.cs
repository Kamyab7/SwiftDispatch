using Microsoft.Extensions.DependencyInjection;
using SwiftDispatch.Contracts;
using SwiftDispatch.Sample.Console.Configuration;
using SwiftDispatch.Sample.Console.Infrastructure;
using SwiftDispatch.Sample.Console.Notifications;
using SwiftDispatch.Sample.Console.Requests;

var services = new ServiceCollection();
services.AddScoped(static _ => new TraceLog());
services.AddSwiftDispatch();

await using ServiceProvider provider = services.BuildServiceProvider();
await using AsyncServiceScope scope = provider.CreateAsyncScope();
IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

string greeting = await mediator.Send(new Hello("Ada"));
await mediator.Send(new SaveGreeting(greeting));
await mediator.Publish(new GreetingSaved(greeting));

System.Console.WriteLine(greeting);
TraceLog trace = scope.ServiceProvider.GetRequiredService<TraceLog>();
System.Console.WriteLine(string.Join(" -> ", trace.Entries));
