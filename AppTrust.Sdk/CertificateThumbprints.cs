using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace AppTrust.Sdk;

public static class CertificateThumbprints
{
    public static string GetSha256Thumbprint(X509Certificate2 certificate)
    {
        var hash = certificate.GetCertHash(HashAlgorithmName.SHA256);
        return Convert.ToHexString(hash);
    }
}
