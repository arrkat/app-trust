using System.Security.Cryptography.X509Certificates;
using AppTrust.Service.Contracts;
using Microsoft.AspNetCore.Http;
using AppTrust.Sdk;

namespace AppTrust.Service.Infrastructure.Strategies;

public class JwtInboundStrategy : IInboundTrustStrategy
{
    private readonly ITokenValidator _validator;

    public JwtInboundStrategy(ITokenValidator validator)
    {
        _validator = validator;
    }

    public async Task<TrustResult> VerifyAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrWhiteSpace(authHeader))
            return new TrustResult(false, null);

        const string bearerPrefix = "Bearer ";
        if (!authHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            return new TrustResult(false, null);

        var token = authHeader[bearerPrefix.Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
            return new TrustResult(false, null);

        var presentedThumbprint = await GetClientCertificateThumbprintAsync(context);
        var result = await _validator.ValidateTokenAsync(token, presentedThumbprint);
        if (!result.IsValid || string.IsNullOrWhiteSpace(result.CallerId))
            return new TrustResult(false, null);

        return new TrustResult(true, result.CallerId);
    }

    private static async Task<string?> GetClientCertificateThumbprintAsync(HttpContext context)
    {
        X509Certificate2? certificate = context.Connection.ClientCertificate
            ?? await context.Connection.GetClientCertificateAsync();
        return certificate is null ? null : CertificateThumbprints.GetSha256Thumbprint(certificate);
    }
}
