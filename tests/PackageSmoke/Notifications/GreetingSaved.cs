using SwiftDispatch.Contracts;

namespace SwiftDispatch.PackageSmoke.Notifications;

public sealed record GreetingSaved(string Greeting) : INotification;
