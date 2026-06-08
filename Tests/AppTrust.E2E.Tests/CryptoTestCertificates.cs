using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AppTrust.Sdk;

namespace AppTrust.E2E.Tests;

internal static class CryptoTestCertificates
{
    public static MtlsTestMaterial CreateOnDisk()
    {
        using var serverRsa = RSA.Create(2048);
        using var clientRsa = RSA.Create(2048);

        var serverCert = CreateCertificate("CN=localhost", serverRsa);
        var clientCert = CreateCertificate($"CN={AppConstants.ClientIdentifier}", clientRsa);

        var directory = Path.Combine(Path.GetTempPath(), $"apptrust-mtls-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        var serverPemPath = Path.Combine(directory, "server.pem");
        var clientPemPath = Path.Combine(directory, "client.pem");

        WriteCertificatePem(serverPemPath, serverCert);
        WriteCertificatePem(clientPemPath, clientCert);

        return new MtlsTestMaterial(directory, serverPemPath, clientPemPath);
    }

    public static TempServerCertificate CreateServerCertificateOnDisk()
    {
        using var rsa = RSA.Create(2048);
        var certificate = CreateCertificate("CN=localhost", rsa);

        var directory = Path.Combine(Path.GetTempPath(), $"apptrust-mtls-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        var serverPemPath = Path.Combine(directory, "server.pem");
        WriteCertificatePem(serverPemPath, certificate);

        return new TempServerCertificate(directory, serverPemPath);
    }

    private static void WriteCertificatePem(string path, X509Certificate2 certificate)
    {
        var privateKey = certificate.GetRSAPrivateKey()
            ?? throw new InvalidOperationException("Certificate must include a private key.");
        File.WriteAllText(path, certificate.ExportCertificatePem() + "\n" + privateKey.ExportPkcs8PrivateKeyPem());
    }

    private static X509Certificate2 CreateCertificate(string subject, RSA rsa)
    {
        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
    }
}

internal sealed class TempServerCertificate : IDisposable
{
    public string ServerPemPath { get; }
    private readonly string _directory;

    public TempServerCertificate(string directory, string serverPemPath)
    {
        _directory = directory;
        ServerPemPath = serverPemPath;
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}

internal sealed class MtlsTestMaterial : IDisposable
{
    public string ServerPemPath { get; }
    public string ClientPemPath { get; }
    private readonly string _directory;

    public MtlsTestMaterial(string directory, string serverPemPath, string clientPemPath)
    {
        _directory = directory;
        ServerPemPath = serverPemPath;
        ClientPemPath = clientPemPath;
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
