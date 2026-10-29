using SwiftDispatch.Contracts;
using SwiftDispatch.PackageSmoke.Behaviors;

namespace SwiftDispatch.PackageSmoke.Configuration;

[GenerateSwiftDispatch(
    PipelineBehaviors = new[] { typeof(LoggingBehavior<,>), typeof(ValidationBehavior) })]
internal static partial class DispatchRegistration;
