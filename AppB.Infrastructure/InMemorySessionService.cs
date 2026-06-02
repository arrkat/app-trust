using System.Collections.Concurrent;
using AppB.Contracts;

namespace AppB.Infrastructure;

public class InMemorySessionService : ISessionService
{
    private readonly ConcurrentDictionary<Guid, string> _sessions = new();

    public Guid CreateSession(string callerId)
    {
        var sessionId = Guid.NewGuid();
        _sessions[sessionId] = callerId;
        return sessionId;
    }
}
