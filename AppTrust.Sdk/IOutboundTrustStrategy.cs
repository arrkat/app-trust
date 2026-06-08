namespace AppTrust.Sdk;

public interface IOutboundTrustStrategy
{
    Task ApplyAsync(HttpRequestMessage request);
}
