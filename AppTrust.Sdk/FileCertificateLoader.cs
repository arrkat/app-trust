using System.Security.Cryptography.X509Certificates;

namespace AppTrust.Sdk;

public sealed class FileCertificateLoader : ICertificateLoader, IDisposable
{
    private readonly string _certificateFilePath;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private X509Certificate2? _cachedCertificate;

    public FileCertificateLoader(string certificateFilePath)
    {
        _certificateFilePath = certificateFilePath;
    }

    public async Task<X509Certificate2> LoadCertificateAsync()
    {
        if (_cachedCertificate is not null)
            return _cachedCertificate;

        await _loadLock.WaitAsync();
        try
        {
            if (_cachedCertificate is not null)
                return _cachedCertificate;

            using var loaded = await PemCertificateLoader.LoadCertificateWithPrivateKeyAsync(_certificateFilePath);
            _cachedCertificate = new X509Certificate2(loaded.Export(X509ContentType.Pkcs12));
            return _cachedCertificate;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public void Dispose()
    {
        _cachedCertificate?.Dispose();
        _cachedCertificate = null;
        _loadLock.Dispose();
    }
}
