using System.Security.Cryptography.X509Certificates;

namespace AppTrust.Service.Contracts;

public interface ICertificateValidator
{
    Task<CertificateValidationResult> ValidateCertificateAsync(X509Certificate2 certificate);
}
