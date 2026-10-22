using SwiftDispatch.Contracts;
using SwiftDispatch.Sample.Console.Behaviors;

namespace SwiftDispatch.Sample.Console.Configuration;

[GenerateSwiftDispatch(
    PipelineBehaviors = new[] { typeof(LoggingBehavior<,>), typeof(ValidationBehavior) })]
internal static partial class DispatchRegistration;
