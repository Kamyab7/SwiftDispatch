using SwiftDispatch.Contracts;

namespace SwiftDispatch.Sample.Console.Notifications;

public sealed record GreetingSaved(string Greeting) : INotification;
