using AppTrust.Service.Contracts;
using Microsoft.AspNetCore.Http;
using AppTrust.Sdk;

namespace AppTrust.Service.Infrastructure.Strategies;

public class MtlsInboundStrategy : IInboundTrustStrategy
{
    private readonly ICertificateValidator _validator;

    public MtlsInboundStrategy(ICertificateValidator validator)
    {
        _validator = validator;
    }

    public async Task<TrustResult> VerifyAsync(HttpContext context)
    {
        var certificate = context.Connection.ClientCertificate
            ?? await context.Connection.GetClientCertificateAsync();
        if (certificate is null)
            return new TrustResult(false, null);

        var result = await _validator.ValidateCertificateAsync(certificate);
        if (!result.IsValid || string.IsNullOrWhiteSpace(result.CallerId))
            return new TrustResult(false, null);

        return new TrustResult(true, result.CallerId);
    }
}
