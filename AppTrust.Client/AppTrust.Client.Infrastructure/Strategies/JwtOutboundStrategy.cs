using AppTrust.Client.Contracts;
using AppTrust.Sdk;

namespace AppTrust.Client.Infrastructure.Strategies;

public class JwtOutboundStrategy : IOutboundTrustStrategy
{
    private readonly ITokenProvider _tokenProvider;
    private readonly ICertificateLoader? _certificateLoader;
    private readonly bool _bindCertificateThumbprint;

    public JwtOutboundStrategy(
        ITokenProvider tokenProvider,
        bool bindCertificateThumbprint,
        ICertificateLoader? certificateLoader = null)
    {
        _tokenProvider = tokenProvider;
        _bindCertificateThumbprint = bindCertificateThumbprint;
        _certificateLoader = certificateLoader;
    }

    public async Task ApplyAsync(HttpRequestMessage request)
    {
        string? thumbprint = null;
        if (_bindCertificateThumbprint)
        {
            if (_certificateLoader is null)
                throw new InvalidOperationException("Certificate loader is required when binding JWT to mTLS certificate.");

            var certificate = await _certificateLoader.LoadCertificateAsync();
            thumbprint = CertificateThumbprints.GetSha256Thumbprint(certificate);
        }

        var token = await _tokenProvider.GetTokenAsync(thumbprint);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }
}
