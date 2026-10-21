using SwiftDispatch.Contracts;
using SwiftDispatch.Sample.Behaviors;

namespace SwiftDispatch.Sample.Configuration;

[GenerateSwiftDispatch(
    PipelineBehaviors = new[] { typeof(LoggingBehavior<,>), typeof(ValidationBehavior) })]
internal static partial class DispatchRegistration;
