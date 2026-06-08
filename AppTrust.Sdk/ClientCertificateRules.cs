using System.Security.Cryptography.X509Certificates;

namespace AppTrust.Sdk;

public static class ClientCertificateRules
{
    public static bool IsValidClientCertificate(
        X509Certificate2? certificate,
        IReadOnlyList<string> allowedSubjects,
        DateTime? utcNow = null)
    {
        if (certificate is null)
            return false;

        var now = utcNow ?? DateTime.UtcNow;
        if (now < certificate.NotBefore.ToUniversalTime() || now > certificate.NotAfter.ToUniversalTime())
            return false;

        var subject = certificate.GetNameInfo(X509NameType.SimpleName, forIssuer: false);
        if (string.IsNullOrWhiteSpace(subject))
            return false;

        return allowedSubjects.Contains(subject, StringComparer.Ordinal);
    }
}
