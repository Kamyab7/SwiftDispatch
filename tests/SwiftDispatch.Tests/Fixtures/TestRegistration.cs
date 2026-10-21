using SwiftDispatch.Contracts;
using SwiftDispatch.Tests.Fixtures.Behaviors;

namespace SwiftDispatch.Tests.Fixtures;

[GenerateSwiftDispatch(PipelineBehaviors = new[] { typeof(Outer<,>), typeof(Inner<,>) })]
internal static partial class TestRegistration;
