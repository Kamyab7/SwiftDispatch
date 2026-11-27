using Xunit;

namespace TrueMediator.Tests.Runtime;

public sealed class NetStandardConsumerTests
{
    [Fact]
    public Task NetStandardConsumerDispatchesRequestsAndStreams() => NetStandardSmoke.Smoke.Run();
}
