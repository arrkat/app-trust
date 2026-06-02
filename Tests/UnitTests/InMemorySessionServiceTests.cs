using AppB.Infrastructure;
using Shared.Infrastructure;

namespace UnitTests;

public class InMemorySessionServiceTests
{
    [Fact]
    public void CreateSession_ReturnsUniqueNonEmptyGuids()
    {
        var service = new InMemorySessionService();

        var session1 = service.CreateSession(AppConstants.AppAIdentifier);
        var session2 = service.CreateSession(AppConstants.AppAIdentifier);

        Assert.NotEqual(Guid.Empty, session1);
        Assert.NotEqual(Guid.Empty, session2);
        Assert.NotEqual(session1, session2);
    }
}
