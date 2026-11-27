using TrueMediator.Contracts;

namespace TrueMediator.PackageSmoke.Notifications;

public sealed record GreetingSaved(string Greeting) : INotification;
