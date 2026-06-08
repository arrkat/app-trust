using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace AppTrust.Sdk;

public sealed class CachedRsaKeyLoader : IDisposable
{
    private readonly IKeyLoader _keyLoader;
    private readonly ILogger<CachedRsaKeyLoader> _logger;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private RsaSecurityKey? _cachedKey;

    public CachedRsaKeyLoader(IKeyLoader keyLoader, ILogger<CachedRsaKeyLoader> logger)
    {
        _keyLoader = keyLoader;
        _logger = logger;
    }

    public async Task<RsaSecurityKey> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedKey is not null)
            return _cachedKey;

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedKey is not null)
                return _cachedKey;

            var pem = await _keyLoader.LoadKeyPemAsync();
            var rsa = RSA.Create();
            rsa.ImportFromPem(pem);

            _cachedKey = new RsaSecurityKey(rsa);
            _logger.LogInformation("RSA key successfully loaded into memory cache.");
            return _cachedKey;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to load RSA key from PEM.");
            throw new InvalidOperationException("Cryptographic key load failed.", ex);
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public void Dispose()
    {
        if (_cachedKey?.Rsa is System.Security.Cryptography.RSA rsa)
            rsa.Dispose();

        _loadLock.Dispose();
    }
}
