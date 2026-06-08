using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using AppTrust.Service.Contracts;
using AppTrust.Sdk;

namespace AppTrust.Service.Infrastructure;

public class JwtTokenValidator : ITokenValidator
{
    private readonly CachedRsaKeyLoader _keyCache;
    private readonly AppTrustServiceOptions _options;
    private readonly IJtiReplayCache _jtiReplayCache;
    private readonly ILogger<JwtTokenValidator> _logger;

    public JwtTokenValidator(
        CachedRsaKeyLoader keyCache,
        IOptions<AppTrustServiceOptions> options,
        IJtiReplayCache jtiReplayCache,
        ILogger<JwtTokenValidator> logger)
    {
        _keyCache = keyCache;
        _options = options.Value;
        _jtiReplayCache = jtiReplayCache;
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

    public async Task<AppTrust.Service.Contracts.TokenValidationResult> ValidateTokenAsync(
        string token,
        string? presentedCertificateThumbprint = null)
    {
        var handler = new JwtSecurityTokenHandler();
        try
        {
            var publicKey = await _keyCache.LoadAsync();
            var expectedCallerId = string.IsNullOrWhiteSpace(_options.ExpectedJwtCallerId)
                ? AppConstants.ClientIdentifier
                : _options.ExpectedJwtCallerId;

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = expectedCallerId,
                ValidateAudience = true,
                ValidAudience = AppConstants.ServiceIdentifier,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = publicKey
            };

            var principal = handler.ValidateToken(token, validationParams, out var validatedToken);
            var jwt = (JwtSecurityToken)validatedToken;

            var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (string.IsNullOrWhiteSpace(jti)
                || !_jtiReplayCache.TryConsume(jti, jwt.ValidTo.ToUniversalTime()))
            {
                _logger.LogWarning("Token validation failed: jti missing or replay detected.");
                return new AppTrust.Service.Contracts.TokenValidationResult(false, null);
            }

            var boundThumbprint = principal.FindFirst(AppConstants.CertThumbprintClaimType)?.Value;
            if (!string.IsNullOrWhiteSpace(boundThumbprint))
            {
                if (string.IsNullOrWhiteSpace(presentedCertificateThumbprint)
                    || !string.Equals(boundThumbprint, presentedCertificateThumbprint, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Token validation failed: certificate thumbprint binding mismatch.");
                    return new AppTrust.Service.Contracts.TokenValidationResult(false, null);
                }
            }

            var callerId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return new AppTrust.Service.Contracts.TokenValidationResult(true, callerId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed during request handling.");
            return new AppTrust.Service.Contracts.TokenValidationResult(false, null);
        }
    }
}
