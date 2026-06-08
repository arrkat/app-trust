namespace AppTrust.Sdk;

public sealed class NoOpOutboundStrategy : IOutboundTrustStrategy
{
    public Task ApplyAsync(HttpRequestMessage request) => Task.CompletedTask;
}
