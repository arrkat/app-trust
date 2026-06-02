using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using AppB.Contracts;
using Shared.Infrastructure;

namespace AppB.Infrastructure;

public class JwtTokenValidator : ITokenValidator
{
    private readonly IKeyLoader _keyLoader;
    private readonly ILogger<JwtTokenValidator> _logger;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private RsaSecurityKey? _cachedKey;

    public JwtTokenValidator(IKeyLoader keyLoader, ILogger<JwtTokenValidator> logger)
    {
        _keyLoader = keyLoader;
        _logger = logger;
    }

    public async Task WarmupAsync()
    {
        try
        {
            await EnsureKeyLoadedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Key warmup failed at startup; will retry on first request.");
        }
    }

    public async Task<AppB.Contracts.TokenValidationResult> ValidateTokenAsync(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        try
        {
            var publicKey = await EnsureKeyLoadedAsync();

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = AppConstants.AppAIdentifier,
                ValidateAudience = true,
                ValidAudience = AppConstants.AppBIdentifier,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = publicKey
            };

            var principal = handler.ValidateToken(token, validationParams, out _);
            var callerId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return new AppB.Contracts.TokenValidationResult(true, callerId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed during request handling.");
            return new AppB.Contracts.TokenValidationResult(false, null);
        }
    }

    private async Task<RsaSecurityKey> EnsureKeyLoadedAsync()
    {
        if (_cachedKey != null)
            return _cachedKey;

        await _loadLock.WaitAsync();
        try
        {
            if (_cachedKey != null)
                return _cachedKey;

            var pem = await _keyLoader.LoadKeyPemAsync();
            var rsa = RSA.Create();
            rsa.ImportFromPem(pem);

            _cachedKey = new RsaSecurityKey(rsa);
            _logger.LogInformation("Public verification key successfully loaded into memory cache.");
            return _cachedKey;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Runtime failure: Unable to load verification public key.");
            throw new InvalidOperationException("Validation subsystem failure.", ex);
        }
        finally
        {
            _loadLock.Release();
        }
    }
}
