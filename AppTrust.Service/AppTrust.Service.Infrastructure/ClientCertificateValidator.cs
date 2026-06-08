using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AppTrust.Service.Contracts;
using AppTrust.Sdk;

namespace AppTrust.Service.Infrastructure;

public class ClientCertificateValidator : ICertificateValidator
{
    private readonly AppTrustServiceOptions _options;
    private readonly ILogger<ClientCertificateValidator> _logger;

    public ClientCertificateValidator(IOptions<AppTrustServiceOptions> options, ILogger<ClientCertificateValidator> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<CertificateValidationResult> ValidateCertificateAsync(X509Certificate2 certificate)
    {
        try
        {
            if (!ClientCertificateRules.IsValidClientCertificate(
                    certificate,
                    _options.AllowedClientCertificateSubjects))
            {
                _logger.LogWarning(
                    "Certificate validation failed: certificate is invalid, expired, or subject is not allowed.");
                return Task.FromResult(new CertificateValidationResult(false, null));
            }

            var callerId = certificate.GetNameInfo(X509NameType.SimpleName, forIssuer: false);
            return Task.FromResult(new CertificateValidationResult(true, callerId));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Certificate validation failed during request handling.");
            return Task.FromResult(new CertificateValidationResult(false, null));
        }
    }
}
