using Microsoft.Extensions.DependencyInjection;
using SwiftDispatch.Contracts;
using SwiftDispatch.Sample.Configuration;
using SwiftDispatch.Sample.Infrastructure;
using SwiftDispatch.Sample.Notifications;
using SwiftDispatch.Sample.Requests;

var services = new ServiceCollection();
services.AddScoped(static _ => new TraceLog());
services.AddSwiftDispatch();

await using ServiceProvider provider = services.BuildServiceProvider();
await using AsyncServiceScope scope = provider.CreateAsyncScope();
IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

string greeting = await mediator.Send(new Hello("Ada"));
await mediator.Send(new SaveGreeting(greeting));
await mediator.Publish(new GreetingSaved(greeting));

Console.WriteLine(greeting);
TraceLog trace = scope.ServiceProvider.GetRequiredService<TraceLog>();
Console.WriteLine(string.Join(" -> ", trace.Entries));
