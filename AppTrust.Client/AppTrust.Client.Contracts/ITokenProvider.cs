
namespace AppTrust.Client.Contracts;

public interface ITokenProvider
{
    Task<string> GetTokenAsync(string? boundCertificateThumbprint = null);
}
