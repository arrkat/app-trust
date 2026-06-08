using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using AppTrust.Client.Contracts;
using AppTrust.Sdk;

namespace AppTrust.Client.Infrastructure;

public class AsymmetricJwtProvider : ITokenProvider
{
    private readonly CachedRsaKeyLoader _keyCache;
    private readonly AppTrustClientOptions _options;
    private readonly ILogger<AsymmetricJwtProvider> _logger;

    public AsymmetricJwtProvider(
        CachedRsaKeyLoader keyCache,
        IOptions<AppTrustClientOptions> options,
        ILogger<AsymmetricJwtProvider> logger)
    {
        _keyCache = keyCache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task WarmupAsync()
    {
        try
        {
            await _keyCache.LoadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Key warmup failed at startup; will retry on first request.");
        }
    }

    public async Task<string> GetTokenAsync(string? boundCertificateThumbprint = null)
    {
        try
        {
            var privateKey = await _keyCache.LoadAsync();
            var credentials = new SigningCredentials(privateKey, SecurityAlgorithms.RsaSha256);
            var callerId = string.IsNullOrWhiteSpace(_options.CallerId)
                ? AppConstants.ClientIdentifier
                : _options.CallerId;

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, callerId),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (!string.IsNullOrWhiteSpace(boundCertificateThumbprint))
                claims.Add(new Claim(AppConstants.CertThumbprintClaimType, boundCertificateThumbprint));

            var token = new JwtSecurityToken(
                issuer: callerId,
                audience: AppConstants.ServiceIdentifier,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token generation failed during runtime.");
            throw;
        }
    }
}
