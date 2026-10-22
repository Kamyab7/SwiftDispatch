using SwiftDispatch.Contracts;
using SwiftDispatch.Tests.Fixtures.Behaviors;

namespace SwiftDispatch.Tests.Fixtures;

[GenerateSwiftDispatch(PipelineBehaviors = new[] { typeof(Outer<,>), typeof(Inner<,>) },
    StreamPipelineBehaviors = new[] { typeof(SwiftDispatch.Tests.Runtime.StreamBehavior) })]
internal static partial class TestRegistration;
