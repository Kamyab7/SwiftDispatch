using Xunit;

namespace SwiftDispatch.Tests.Runtime;

public sealed class NetStandardConsumerTests
{
    [Fact]
    public Task NetStandardConsumerDispatchesRequestsAndStreams() => NetStandardSmoke.Smoke.Run();
}
