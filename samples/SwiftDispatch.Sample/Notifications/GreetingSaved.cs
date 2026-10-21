using SwiftDispatch.Contracts;

namespace SwiftDispatch.Sample.Notifications;

public sealed record GreetingSaved(string Greeting) : INotification;
