namespace AppTrust.Service.Contracts;

public interface IJtiReplayCache
{
    /// <summary>Returns false when the jti was already consumed (replay).</summary>
    bool TryConsume(string jti, DateTime expiresAtUtc);
}
