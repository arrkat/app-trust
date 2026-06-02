using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using AppA.Contracts;
using Shared.Infrastructure;

namespace AppA.Infrastructure;

public class AsymmetricJwtProvider : ITokenProvider
{
    private readonly IKeyLoader _keyLoader;
    private readonly ILogger<AsymmetricJwtProvider> _logger;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private RsaSecurityKey? _cachedKey;

    public AsymmetricJwtProvider(IKeyLoader keyLoader, ILogger<AsymmetricJwtProvider> logger)
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

    public async Task<string> GetTokenAsync()
    {
        try
        {
            var privateKey = await EnsureKeyLoadedAsync();
            var credentials = new SigningCredentials(privateKey, SecurityAlgorithms.RsaSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, AppConstants.AppAIdentifier),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: AppConstants.AppAIdentifier,
                audience: AppConstants.AppBIdentifier,
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
            return _cachedKey;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to load cryptographic private key.");
            throw new InvalidOperationException("Cryptographic subsystem initialization failed.", ex);
        }
        finally
        {
            _loadLock.Release();
        }
    }
}
