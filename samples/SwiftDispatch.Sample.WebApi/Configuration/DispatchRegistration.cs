using SwiftDispatch.Contracts;
using SwiftDispatch.Sample.WebApi.Behaviors;

namespace SwiftDispatch.Sample.WebApi.Configuration;

[GenerateSwiftDispatch(PipelineBehaviors = new[] { typeof(LoggingBehavior<,>) })]
internal static partial class DispatchRegistration;
