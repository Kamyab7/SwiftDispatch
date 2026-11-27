using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using TrueMediator.Contracts;
using TrueMediator.Sample.Console;

var services = new ServiceCollection();
services.AddTrueMediator(options => options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

await using ServiceProvider provider = services.BuildServiceProvider();
await using AsyncServiceScope scope = provider.CreateAsyncScope();
IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

string name = args.Length > 0 ? args[0] : "Ada";
string greeting = await mediator.Send(new Hello(name));
Console.WriteLine(greeting);
await mediator.Publish(new GreetingCreated(greeting));
