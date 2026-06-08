using System.Security.Cryptography.X509Certificates;

namespace AppTrust.Sdk;

public interface ICertificateLoader
{
    Task<X509Certificate2> LoadCertificateAsync();
}
